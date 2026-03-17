// متغيرات عامة للطباعة
let defaultPrinter = localStorage.getItem('defaultPrinter');
let printDialogShown = false;

// دالة عرض مربع حوار الطباعة
function showPrintDialog(pdfBlob, fileName, onConfirm, onCancel) {
    // إزالة أي مربع حوار قديم
    const oldDialog = document.getElementById('printDialog');
    if (oldDialog) oldDialog.remove();

    // إنشاء مربع الحوار
    const dialog = document.createElement('div');
    dialog.id = 'printDialog';
    dialog.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0,0,0,0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
        direction: rtl;
        font-family: 'Segoe UI', Tahoma, sans-serif;
    `;

    dialog.innerHTML = `
        <div style="background: white; border-radius: 16px; padding: 25px; max-width: 400px; width: 90%; box-shadow: 0 20px 40px rgba(0,0,0,0.2);">
            <div style="text-align: center; margin-bottom: 20px;">
                <i class="fas fa-print" style="font-size: 48px; color: #4361ee;"></i>
                <h3 style="margin: 15px 0 5px; color: #333;">طباعة المستند</h3>
                <p style="color: #666; font-size: 14px;">${fileName}</p>
            </div>
            
            <div id="printerList" style="margin: 20px 0; max-height: 200px; overflow-y: auto; display: none;">
                <label style="display: block; margin-bottom: 10px; color: #333; font-weight: 500;">اختر الطابعة:</label>
                <div id="printersContainer"></div>
            </div>
            
            <div id="printerError" style="background: #f8d7da; color: #721c24; padding: 12px; border-radius: 8px; margin: 15px 0; display: none; text-align: center;">
                <i class="fas fa-exclamation-circle"></i>
                <span>حدث خطأ في الطباعة، يرجى اختيار طابعة أخرى</span>
            </div>
            
            <div style="display: flex; gap: 10px; justify-content: center; flex-wrap: wrap;">
                <button onclick="cancelPrint()" class="dialog-btn cancel" style="background: #6c757d; color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; display: inline-flex; align-items: center; gap: 5px;">
                    <i class="fas fa-times"></i> إلغاء
                </button>
                <button onclick="changePrinter()" class="dialog-btn change" style="background: #ffc107; color: #333; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; display: inline-flex; align-items: center; gap: 5px;">
                    <i class="fas fa-print"></i> تغيير الطابعة
                </button>
                <button onclick="confirmPrint()" class="dialog-btn confirm" style="background: #28a745; color: white; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; display: inline-flex; align-items: center; gap: 5px;">
                    <i class="fas fa-check"></i> طباعة
                </button>
            </div>
            
            <div style="margin-top: 15px; font-size: 12px; color: #999; text-align: center;">
                <i class="fas fa-info-circle"></i> اضغط Enter للموافقة، Esc للإلغاء
            </div>
        </div>
    `;

    document.body.appendChild(dialog);

    // حفظ بيانات PDF للاستخدام لاحقاً
    window.currentPrintData = {
        pdfBlob: pdfBlob,
        fileName: fileName,
        onConfirm: onConfirm,
        onCancel: onCancel
    };

    // التحقق من وجود طابعة افتراضية
    checkDefaultPrinter();

    // أحداث لوحة المفاتيح
    dialog.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            confirmPrint();
        } else if (e.key === 'Escape') {
            cancelPrint();
        }
    });
    dialog.focus();

    // جلب قائمة الطابعات
    getPrinters();
}

// دالة جلب قائمة الطابعات
async function getPrinters() {
    try {
        // محاكاة جلب الطابعات (في الواقع نحتاج API خاصة)
        const printers = [
            { id: 1, name: 'HP LaserJet Pro M404dn', isDefault: true },
            { id: 2, name: 'Canon LBP6230dw', isDefault: false },
            { id: 3, name: 'EPSON L3150', isDefault: false },
            { id: 4, name: 'Microsoft Print to PDF', isDefault: false },
            { id: 5, name: 'Fax', isDefault: false }
        ];

        const printersContainer = document.getElementById('printersContainer');
        if (!printersContainer) return;

        printersContainer.innerHTML = printers.map(p => `
            <label style="display: flex; align-items: center; padding: 8px; background: #f8f9fa; border-radius: 8px; margin-bottom: 5px; cursor: pointer;">
                <input type="radio" name="printer" value="${p.name}" ${p.isDefault ? 'checked' : ''} style="margin-left: 10px;">
                <span style="flex: 1;">${p.name}</span>
                ${p.isDefault ? '<span style="color: #27ae60; font-size: 11px;">(افتراضي)</span>' : ''}
            </label>
        `).join('');

    } catch (error) {
        console.error('خطأ في جلب الطابعات:', error);
    }
}

// دالة التحقق من الطابعة الافتراضية
function checkDefaultPrinter() {
    if (defaultPrinter) {
        // استخدام الطابعة المحفوظة
        console.log('استخدام الطابعة الافتراضية:', defaultPrinter);
        document.getElementById('printerList').style.display = 'none';
    } else {
        // إظهار قائمة الطابعات لأول مرة
        document.getElementById('printerList').style.display = 'block';
    }
}

// دالة تأكيد الطباعة
function confirmPrint() {
    const data = window.currentPrintData;
    if (!data) return;

    const selectedPrinter = document.querySelector('input[name="printer"]:checked')?.value || defaultPrinter;

    if (!selectedPrinter) {
        document.getElementById('printerError').style.display = 'block';
        return;
    }

    // حفظ الطابعة كافتراضية
    if (!defaultPrinter) {
        localStorage.setItem('defaultPrinter', selectedPrinter);
        defaultPrinter = selectedPrinter;
    }

    // تنفيذ الطباعة
    printPDF(data.pdfBlob, selectedPrinter);

    // إغلاق مربع الحوار
    document.getElementById('printDialog').remove();

    // إظهار رسالة نجاح
    showToast('success', 'تم إرسال المستند للطباعة بنجاح');
}

// دالة تغيير الطابعة
function changePrinter() {
    defaultPrinter = null;
    localStorage.removeItem('defaultPrinter');
    document.getElementById('printerList').style.display = 'block';
    document.getElementById('printerError').style.display = 'none';
}

// دالة إلغاء الطباعة
function cancelPrint() {
    const data = window.currentPrintData;
    if (data && data.onCancel) data.onCancel();

    document.getElementById('printDialog').remove();
    showToast('info', 'تم إلغاء الطباعة');
}

// دالة الطباعة الفعلية
function printPDF(pdfBlob, printerName) {
    try {
        // إنشاء رابط PDF
        const url = URL.createObjectURL(pdfBlob);

        // فتح PDF في نافذة جديدة للطباعة
        const printWindow = window.open(url, '_blank');

        // بعد تحميل PDF، فتح مربع حوار الطباعة
        printWindow.onload = function () {
            printWindow.print();
        };

        // تنظيف الرابط بعد فترة
        setTimeout(() => URL.revokeObjectURL(url), 60000);

    } catch (error) {
        console.error('خطأ في الطباعة:', error);
        showToast('error', 'حدث خطأ في الطباعة، يرجى المحاولة مرة أخرى');
    }
}

// دالة إظهار رسالة toast
function showToast(type, message) {
    const toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        padding: 15px 25px;
        border-radius: 8px;
        color: white;
        font-size: 14px;
        z-index: 10000;
        animation: slideIn 0.3s ease;
        box-shadow: 0 5px 15px rgba(0,0,0,0.2);
    `;

    switch (type) {
        case 'success':
            toast.style.background = '#28a745';
            toast.innerHTML = `<i class="fas fa-check-circle"></i> ${message}`;
            break;
        case 'error':
            toast.style.background = '#dc3545';
            toast.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${message}`;
            break;
        case 'info':
            toast.style.background = '#17a2b8';
            toast.innerHTML = `<i class="fas fa-info-circle"></i> ${message}`;
            break;
    }

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// إضافة أنماط الحركة
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);