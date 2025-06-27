// Transaction Management JavaScript Functions

// Global variables
let productCache = {};
let rowIndex = 0;

// Initialize transaction management
document.addEventListener('DOMContentLoaded', function () {
    initializeTransactionForm();
    loadProductCache();
});

// Initialize form functionality
function initializeTransactionForm() {
    const form = document.getElementById('transactionForm');
    if (form) {
        // Set initial row index
        rowIndex = document.querySelectorAll('.product-row').length;

        // Initialize existing rows
        const rows = document.querySelectorAll('.product-row');
        rows.forEach((row, index) => {
            const selectElement = row.querySelector('.product-select');
            if (selectElement && selectElement.value) {
                updatePrice(selectElement, index);
            }
        });

        // Form validation
        form.addEventListener('submit', validateTransactionForm);
    }
}

// Load product data into cache
async function loadProductCache() {
    try {
        const response = await fetch('/api/products'); // You might need to create this endpoint
        if (response.ok) {
            const products = await response.json();
            products.forEach(product => {
                productCache[product.id] = product;
            });
        }
    } catch (error) {
        console.log('Could not load product cache:', error);
    }
}

// Update product price and stock information
async function updatePrice(selectElement, index) {
    const productId = selectElement.value;
    const row = selectElement.closest('tr');
    const priceDisplay = row.querySelector('.price-display');
    const unitPriceInput = row.querySelector('.unit-price');
    const quantityInput = row.querySelector('.quantity-input');
    const stockInfo = row.querySelector('.stock-info');

    // Show loading state
    priceDisplay.textContent = 'Loading...';

    if (productId) {
        try {
            // Check cache first
            let productData = productCache[productId];

            if (!productData) {
                // Fetch from server
                const response = await fetch(/Data_Transaksi/GetProductPrice ? productId = ${ productId });
                productData = await response.json();
                productCache[productId] = productData;
            }

            // Update UI
            priceDisplay.textContent = Rp ${ productData.price.toLocaleString('id-ID') };
            unitPriceInput.value = productData.price;
            stockInfo.textContent = Stock: ${ productData.stock };
            stockInfo.className = productData.stock > 0 ? 'stock-info text-success small' : 'stock-info text-danger small';

            // Set quantity constraints
            quantityInput.max = productData.stock;
            quantityInput.min = 1;

            // Validate current quantity
            if (parseInt(quantityInput.value) > productData.stock) {
                quantityInput.value = productData.stock;
                showAlert('warning', Quantity disesuaikan dengan stock yang tersedia(${ productData.stock }));
            }

            calculateTotal(index);
        } catch (error) {
            console.error('Error fetching product price:', error);
            priceDisplay.textContent = 'Error';
            stockInfo.textContent = 'Error loading stock info';
            stockInfo.className = 'stock-info text-danger small';
        }
    } else {
        // Reset values
        priceDisplay.textContent = 'Rp 0';
        unitPriceInput.value = 0;
        stockInfo.textContent = '';
        quantityInput.max = '';
        quantityInput.min = 0;
        calculateTotal(index);
    }
}

// Calculate total for a specific row
function calculateTotal(index) {
    const rows = document.querySelectorAll('.product-row');
    if (index >= rows.length) return;

    const row = rows[index];
    const quantity = parseInt(row.querySelector('.quantity-input').value) || 0;
    const unitPrice = parseFloat(row.querySelector('.unit-price').value) || 0;
    const total = quantity * unitPrice;

    row.querySelector('.total-display').textContent = Rp ${ total.toLocaleString('id-ID') };
    row.querySelector('.item-total').value = total;

    updateGrandTotal();
}

// Update grand total
function updateGrandTotal() {
    let grandTotal = 0;
    document.querySelectorAll('.item-total').forEach(input => {
        grandTotal += parseFloat(input.value) || 0;
    });

    const grandTotalElement = document.getElementById('grandTotal');
    if (grandTotalElement) {
        grandTotalElement.textContent = Rp ${ grandTotal.toLocaleString('id-ID') };

        // Add visual emphasis for large amounts
        if (grandTotal > 1000000) {
            grandTotalElement.className = 'text-success font-weight-bold';
        } else {
            grandTotalElement.className = 'text-primary font-weight-bold';
        }
    }
}

// Add new product row
function addNewRow() {
    const tbody = document.getElementById('productTableBody');
    if (!tbody) return;

    const newRow = document.createElement('tr');
    newRow.className = 'product-row';
    newRow.innerHTML = 
        <td>
            <select name="Items[${rowIndex}].ProductId" class="form-select product-select" onchange="updatePrice(this, ${rowIndex})">
                <option value="">-- Pilih Produk --</option>
                ${getProductOptions()}
            </select>
        </td>
        <td>
            <input name="Items[${rowIndex}].Quantity" class="form-control quantity-input" type="number" min="1" onchange="calculateTotal(${rowIndex})" />
            <div class="stock-info text-muted small"></div>
        </td>
        <td>
            <span class="price-display">Rp 0</span>
            <input type="hidden" class="unit-price" value="0" />
        </td>
        <td>
            <span class="total-display font-weight-bold">Rp 0</span>
            <input type="hidden" class="item-total" value="0" />
        </td>
        <td>
            <button type="button" class="btn btn-sm btn-danger" onclick="removeRow(this)" title="Hapus Baris">
                <i class="fas fa-trash"></i>
            </button>
        </td>
        ;

    tbody.appendChild(newRow);
    rowIndex++;

    // Focus on the new product select
    const newSelect = newRow.querySelector('.product-select');
    if (newSelect) {
        newSelect.focus();
    }
}

// Get product options HTML
function getProductOptions() {
    const productsSelect = document.querySelector('select[name="Items[0].ProductId"]');
    if (productsSelect) {
        return Array.from(productsSelect.options)
            .slice(1) // Skip the first "-- Pilih Produk --" option
            .map(option => <option value="${option.value}">${option.text}</option>)
            .join('');
    }
    return '';
}

// Remove product row
function removeRow(button) {
    const row = button.closest('tr');
    const tbody = row.parentNode;

    // Confirm deletion if row has data
    const productSelect = row.querySelector('.product-select');
    const quantityInput = row.querySelector('.quantity-input');

    if (productSelect.value || quantityInput.value) {
        if (!confirm('Apakah Anda yakin ingin menghapus baris ini?')) {
            return;
        }
    }

    row.remove();
    updateGrandTotal();

    // Ensure at least one row exists
    if (tbody.children.length === 0) {
        addNewRow();
    }
}

// Validate transaction form
function validateTransactionForm(event) {
    const form = event.target;
    let isValid = true;
    let errorMessages = [];

    // Check if user is selected
    const userSelect = form.querySelector('select[name="UserId"]');
    if (!userSelect.value) {
        errorMessages.push('Silakan pilih user');
        isValid = false;
    }

    // Check if at least one product is selected
    const productRows = form.querySelectorAll('.product-row');
    let hasProducts = false;

    productRows.forEach((row, index) => {
        const productSelect = row.querySelector('.product-select');
        const quantityInput = row.querySelector('.quantity-input');

        if (productSelect.value && parseInt(quantityInput.value) > 0) {
            hasProducts = true;

            // Validate stock
            const stockInfo = row.querySelector('.stock-info');
            const maxStock = parseInt(quantityInput.max) || 0;
            const currentQuantity = parseInt(quantityInput.value) || 0;

            if (currentQuantity > maxStock) {
                errorMessages.push(Quantity untuk produk di baris ${ index + 1} melebihi stock yang tersedia);
    isValid = false;
}
        }
    });

if (!hasProducts) {
    errorMessages.push('Silakan pilih minimal satu produk');
    isValid = false;
}

// Show errors if any
if (!isValid) {
    event.preventDefault();
    showAlert('danger', errorMessages.join('<br>'));
}

return isValid;
}

// Show alert message
function showAlert(type, message) {
    // Remove existing alerts
    const existingAlert = document.querySelector('.alert-custom');
    if (existingAlert) {
        existingAlert.remove();
    }

    // Create new alert
    const alertDiv = document.createElement('div');
    alertDiv.className = alert alert - ${ type } alert - dismissible fade show alert - custom;
    alertDiv.innerHTML =
        ${ message }
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        ;

    // Insert at the top of the container
    const container = document.querySelector('.container');
    if (container) {
        container.insertBefore(alertDiv, container.firstChild);

        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 5000);
    }
}
