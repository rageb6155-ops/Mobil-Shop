using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MobileShopSystem.Services
{
    public class WhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly string _phoneNumberId;
        private readonly string _apiVersion;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<WhatsAppService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;

            _accessToken = _configuration["WhatsApp:AccessToken"];
            _phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            _apiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v17.0";

            _httpClient.BaseAddress = new Uri($"https://graph.facebook.com/{_apiVersion}/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }

        // ========== دالة تنظيف رقم الهاتف ==========
        private string CleanPhoneNumber(string phone)
        {
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());
            if (cleaned.StartsWith("0"))
                cleaned = "20" + cleaned.Substring(1);
            return cleaned;
        }

        // ========== دالة أساسية لإرسال رسالة واحدة ==========
        public async Task<WhatsAppResponse> SendSingleMessage(string to, string message)
        {
            try
            {
                to = CleanPhoneNumber(to);

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = to,
                    type = "text",
                    text = new
                    {
                        preview_url = false,
                        body = message
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var messageId = jsonDoc.RootElement
                        .GetProperty("messages")[0]
                        .GetProperty("id")
                        .GetString();

                    _logger.LogInformation("✅ WhatsApp message sent successfully to {Phone}, ID: {MessageId}", to, messageId);

                    return new WhatsAppResponse
                    {
                        Success = true,
                        MessageId = messageId,
                        Timestamp = DateTime.Now
                    };
                }
                else
                {
                    _logger.LogError("❌ WhatsApp API error: {Error}", responseContent);

                    return new WhatsAppResponse
                    {
                        Success = false,
                        Error = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in WhatsAppService");

                return new WhatsAppResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        // ========== دالة تقسيم الرسائل الطويلة (1000 حرف كحد أقصى للجزء) ==========
        public async Task<WhatsAppResponse> SendLongMessage(string to, string longMessage)
        {
            const int MAX_LENGTH = 1000; // 1000 حرف كحد أقصى للجزء الواحد

            // لو الرسالة قصيرة، ابعتها عادي
            if (longMessage.Length <= MAX_LENGTH)
            {
                return await SendSingleMessage(to, longMessage);
            }

            // تقطيع الرسالة لأجزاء (كل جزء 1000 حرف)
            var parts = new List<string>();
            int totalParts = (int)Math.Ceiling((double)longMessage.Length / MAX_LENGTH);

            for (int i = 0; i < totalParts; i++)
            {
                int startIndex = i * MAX_LENGTH;
                int length = Math.Min(MAX_LENGTH, longMessage.Length - startIndex);
                string part = longMessage.Substring(startIndex, length);

                // إضافة رقم الجزء في أول رسالة وآخر رسالة
                if (i == 0)
                {
                    part = $"📨 رسالة من {totalParts} أجزاء (الجزء 1/{totalParts})\n\n" + part;
                }
                else if (i == totalParts - 1)
                {
                    part = part + $"\n\n📨 (الجزء {i + 1}/{totalParts}) - نهاية الرسالة";
                }
                else
                {
                    part = $"📨 (الجزء {i + 1}/{totalParts})\n\n" + part;
                }

                parts.Add(part);
            }

            _logger.LogInformation("📨 تقسيم الرسالة إلى {Count} أجزاء", parts.Count);

            // إرسال الأجزاء بالتتابع
            WhatsAppResponse lastResponse = null;
            bool allSuccess = true;
            List<string> messageIds = new List<string>();

            for (int i = 0; i < parts.Count; i++)
            {
                _logger.LogInformation("📤 إرسال الجزء {Current} من {Total}", i + 1, parts.Count);

                lastResponse = await SendSingleMessage(to, parts[i]);

                if (lastResponse.Success)
                {
                    messageIds.Add(lastResponse.MessageId);
                }
                else
                {
                    allSuccess = false;
                    _logger.LogError("❌ فشل إرسال الجزء {Current}: {Error}", i + 1, lastResponse.Error);
                }

                // انتظار ثانية بين كل جزء (عشان ما يوقفش الحساب)
                if (i < parts.Count - 1)
                {
                    await Task.Delay(1000);
                }
            }

            return new WhatsAppResponse
            {
                Success = allSuccess,
                MessageId = string.Join(",", messageIds),
                Timestamp = DateTime.Now,
                Error = allSuccess ? null : "بعض الأجزاء فشلت في الإرسال"
            };
        }

        // ========== إرسال صورة عبر واتساب ==========
        public async Task<WhatsAppResponse> SendImageMessage(string to, string imageUrl, string caption = "")
        {
            try
            {
                to = CleanPhoneNumber(to);

                var payload = new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = to,
                    type = "image",
                    image = new
                    {
                        link = imageUrl,
                        caption = caption
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var messageId = jsonDoc.RootElement
                        .GetProperty("messages")[0]
                        .GetProperty("id")
                        .GetString();

                    _logger.LogInformation("✅ WhatsApp image sent successfully to {Phone}, ID: {MessageId}", to, messageId);

                    return new WhatsAppResponse
                    {
                        Success = true,
                        MessageId = messageId,
                        Timestamp = DateTime.Now
                    };
                }
                else
                {
                    _logger.LogError("❌ WhatsApp image API error: {Error}", responseContent);

                    return new WhatsAppResponse
                    {
                        Success = false,
                        Error = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in SendImageMessage");

                return new WhatsAppResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        // ========== رسالة استلام جهاز ==========
        public async Task<WhatsAppResponse> SendDeviceReceivedMessage(string to, string customerName,
            string deviceModel, string deviceCode, DateTime receivedDate)
        {
            var message = $"📱 *تم استلام جهازك بنجاح*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"تم استلام جهازك {deviceModel} للصيانة.\n\n";
            message += $"📌 *كود المتابعة:* {deviceCode}\n";
            message += $"📅 *تاريخ الاستلام:* {receivedDate:yyyy-MM-dd}\n";
            message += $"⏰ *وقت الاستلام:* {receivedDate:hh:mm tt}\n\n";
            message += $"سنقوم بإعلامك بأي تحديثات.\n";
            message += $"شكراً لثقتك بنا 🙏\n\n";
            message += $"📍 مركز الصيانة - MobileShop\n";
            message += $"📞 للاستفسار: 01064211484";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة تغيير الحالة ==========
        public async Task<WhatsAppResponse> SendStatusChangeMessage(string to, string customerName,
            string deviceModel, string oldStatus, string newStatus, string notes = "")
        {
            string statusEmoji = GetStatusEmoji(newStatus);

            var message = $"🔄 *تحديث حالة جهازك*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"جهازك {deviceModel}:\n\n";
            message += $"📦 الحالة السابقة: {oldStatus}\n";
            message += $"{statusEmoji} الحالة الجديدة: *{newStatus}*\n\n";

            if (!string.IsNullOrEmpty(notes))
            {
                message += $"📝 *ملاحظات الفني:*\n{notes}\n\n";
            }

            message += $"لمتابعة حالة جهازك، يمكنك التواصل معنا:\n";
            message += $"📞 01064211484\n\n";
            message += $"شكراً لثقتك بنا ✨";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة اكتمال الصيانة ==========
        public async Task<WhatsAppResponse> SendRepairCompletedMessage(string to, string customerName,
            string deviceModel, decimal finalCost, decimal paid, decimal remaining)
        {
            var message = $"✅ *تم اكتمال صيانة جهازك*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"تم الانتهاء من صيانة جهازك {deviceModel}.\n\n";
            message += $"💰 *تفاصيل الدفع:*\n";
            message += $"┌─────────────────────\n";
            message += $"│ التكلفة النهائية: {finalCost:N0} ج.م\n";
            message += $"│ المدفوع: {paid:N0} ج.م\n";
            message += $"│ المتبقي: {remaining:N0} ج.م\n";
            message += $"└─────────────────────\n\n";

            if (remaining > 0)
            {
                message += $"⚠️ يرجى التفضل بزيارة المحل لاستلام الجهاز وسداد المتبقي.\n\n";
            }
            else
            {
                message += $"✅ يمكنك القدوم لاستلام جهازك في أي وقت.\n\n";
            }

            message += $"📍 مركز الصيانة - MobileShop\n";
            message += $"📞 للاستفسار: 01064211484\n\n";
            message += $"شكراً لثقتك بنا 🙏";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة تذكير ==========
        public async Task<WhatsAppResponse> SendReminderMessage(string to, string customerName,
            string deviceModel, decimal remaining, DateTime? promisedDate)
        {
            var message = $"⏰ *تذكير بمتابعة جهازك*\n\n";
            message += $"مرحباً {customerName}،\n";

            if (promisedDate.HasValue)
            {
                var daysLeft = (promisedDate.Value - DateTime.Now).Days;
                message += $"جهازك {deviceModel} ";
                message += daysLeft > 0 ? $"متوقع الانتهاء بعد {daysLeft} أيام" : "من المفترض أن يكون جاهزاً اليوم";
                message += "\n\n";
            }

            if (remaining > 0)
            {
                message += $"💰 المتبقي عليك: {remaining:N0} ج.م\n\n";
            }

            message += $"لمتابعة حالة جهازك، يمكنك الرد على هذه الرسالة أو الاتصال بنا.\n";
            message += $"📞 01064211484\n\n";
            message += $"مع تحيات فريق الصيانة ✨";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة إيصال دفع ==========
        public async Task<WhatsAppResponse> SendPaymentReceipt(string to, string customerName,
            string deviceModel, decimal amount, string paymentMethod, decimal remaining)
        {
            var message = $"💰 *إيصال دفع*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"تم استلام مبلغ {amount:N0} ج.م عن طريق {paymentMethod}\n";
            message += $"لجهازك {deviceModel}\n\n";
            message += $"المتبقي عليك: {remaining:N0} ج.م\n\n";
            message += $"شكراً لتعاملكم معنا ✨\n";
            message += $"📞 01064211484";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة ترحيب ==========
        public async Task<WhatsAppResponse> SendWelcomeMessage(string to, string customerName)
        {
            var message = $"👋 *مرحباً بك في مركز الصيانة*\n\n";
            message += $"عزيزي {customerName}،\n";
            message += $"نشكرك على ثقتك بنا. يمكنك متابعة حالة أجهزتك عبر هذا الرقم.\n\n";
            message += $"للتواصل والاستفسار:\n";
            message += $"📞 01064211484\n";
            message += $"🕐 أوقات العمل: 10 صباحاً - 10 مساءً\n\n";
            message += $"مع تحيات فريق العمل 🌟";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة إضافة قطع غيار ==========
        public async Task<WhatsAppResponse> SendSparePartAddedMessage(string to, string customerName,
            string deviceModel, string partName, int quantity, decimal cost, decimal totalCost, decimal remaining)
        {
            var message = $"🔧 *تم إضافة قطع غيار لجهازك*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"تم إضافة قطع غيار لجهازك {deviceModel}:\n\n";
            message += $"• {partName} - الكمية: {quantity}\n";
            message += $"💰 التكلفة الإضافية: {totalCost:N0} ج.م\n\n";

            message += $"💰 إجمالي التكلفة حتى الآن: {cost:N0} ج.م\n";
            message += $"💵 المدفوع: {0:N0} ج.م\n";
            message += $"📊 المتبقي: {remaining:N0} ج.م\n\n";

            message += $"سنقوم بإعلامك عند الانتهاء من الصيانة.\n";
            message += $"شكراً لثقتك بنا 🙏\n\n";
            message += $"📞 01064211484";

            return await SendLongMessage(to, message);
        }

        // ========== رسالة إنشاء ضمان ==========
        public async Task<WhatsAppResponse> SendWarrantyCreatedMessage(string to, string customerName,
            string deviceModel, string warrantyNumber, string warrantyType, DateTime startDate, DateTime endDate, string coverage)
        {
            var message = $"🛡️ *تم إضافة ضمان لجهازك*\n\n";
            message += $"مرحباً {customerName}،\n";
            message += $"تم إضافة ضمان لجهازك {deviceModel}\n\n";
            message += $"📋 *تفاصيل الضمان:*\n";
            message += $"رقم الضمان: {warrantyNumber}\n";
            message += $"نوع الضمان: {warrantyType}\n";
            message += $"تاريخ البداية: {startDate:yyyy-MM-dd}\n";
            message += $"تاريخ النهاية: {endDate:yyyy-MM-dd}\n";
            message += $"المدة: {(endDate - startDate).Days} يوم\n\n";

            if (!string.IsNullOrEmpty(coverage))
            {
                message += $"📝 التغطية: {coverage}\n\n";
            }

            message += $"للتواصل: 01064211484\n";
            message += $"مع تحيات فريق الصيانة ✨";

            return await SendLongMessage(to, message);
        }

        // ========== دالة مساعدة للحصول على الإيموجي ==========
        private string GetStatusEmoji(string status)
        {
            return status switch
            {
                "مستلم" => "📦",
                "قيد الصيانة" => "🔧",
                "بانتظار قطع غيار" => "⏳",
                "تم الاصلاح" => "✅",
                "تم التسليم" => "🎉",
                _ => "📌"
            };
        }
    }

    public class WhatsAppResponse
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }
}