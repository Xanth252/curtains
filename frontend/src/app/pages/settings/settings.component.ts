import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HeaderComponent } from '../../components/header/header.component';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { BankDetails, SavedParticular } from '../../models/models';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, HeaderComponent],
  template: `
    <div class="page-wrapper area-settings">
      <app-header />
      <main class="main-content settings-page">
        <div class="settings-heading">
          <div>
            <div class="settings-eyebrow">PREFERENCES</div>
            <h1>Settings</h1>
            <p>Manage your sign-in details and the payment information printed on quotations.</p>
          </div>
        </div>

        <section class="settings-card">
          <div class="settings-card-heading">
            <div class="settings-icon">👤</div>
            <div>
              <h2>Account &amp; security</h2>
              <p>Change your username or password. Enter your current password to confirm any changes.</p>
            </div>
          </div>
          <form (ngSubmit)="saveAccount()" #accountForm="ngForm">
            <div class="settings-form-grid">
              <label class="settings-field">
                <span>Username</span>
                <input class="form-control" name="username" [(ngModel)]="account.username" required autocomplete="username" />
              </label>
              <label class="settings-field">
                <span>Current password</span>
                <input class="form-control" name="currentPassword" [(ngModel)]="account.currentPassword" type="password" required autocomplete="current-password" placeholder="Confirm changes" />
              </label>
              <label class="settings-field">
                <span>New password <small>(optional)</small></span>
                <input class="form-control" name="newPassword" [(ngModel)]="account.newPassword" type="password" minlength="8" autocomplete="new-password" placeholder="At least 8 characters" />
              </label>
              <label class="settings-field">
                <span>Confirm new password</span>
                <input class="form-control" name="confirmPassword" [(ngModel)]="account.confirmPassword" type="password" autocomplete="new-password" placeholder="Re-enter new password" />
              </label>
            </div>
            <div class="settings-card-footer">
              <span class="settings-hint">Leave both new password fields empty to keep your current password.</span>
              <button class="btn btn-primary" type="submit" [disabled]="savingAccount || accountForm.invalid">
                {{ savingAccount ? 'Saving…' : 'Save account settings' }}
              </button>
            </div>
          </form>
        </section>

        <section class="settings-card">
          <button class="settings-card-heading products-toggle" type="button" (click)="toggleProducts()" [attr.aria-expanded]="showProducts">
            <div class="settings-icon product-icon">📦</div>
            <div class="products-heading-copy">
              <h2>Products</h2>
              <p>Manage saved products and particulars used in quotation maker.</p>
            </div>
            <span class="products-toggle-icon" aria-hidden="true">{{ showProducts ? '−' : '+' }}</span>
          </button>
          <ng-container *ngIf="showProducts">
          <label class="settings-field product-search"><span>Search products</span><input class="form-control" name="productSearch" [(ngModel)]="productSearch" placeholder="Search by product name" /></label>
          <form class="product-create-form" (ngSubmit)="addProduct()">
            <label class="settings-field product-name-field"><span>Product or particular</span><input class="form-control" name="newProductName" [(ngModel)]="newProduct.particular" required placeholder="e.g. Curtain Fabric" /></label>
            <label class="settings-field"><span>Unit</span><input class="form-control" name="newProductUnit" [(ngModel)]="newProduct.defaultUnit" list="product-units" required placeholder="e.g. mtr" /></label>
            <label class="settings-field"><span>Default rate (optional)</span><input class="form-control" name="newProductRate" [(ngModel)]="newProduct.defaultRate" type="number" min="0" step="0.01" placeholder="0" /></label>
            <button class="btn btn-primary" type="submit" [disabled]="savingProduct || !newProduct.particular.trim() || !newProduct.defaultUnit.trim()">{{ savingProduct ? 'Adding…' : 'Add product' }}</button>
          </form>
          <datalist id="product-units"><option *ngFor="let unit of availableUnits" [value]="unit"></option></datalist>
          <div class="product-list" *ngIf="filteredProducts.length; else emptyProductResults">
            <div class="product-list-heading"><span>Saved products</span><span>{{ filteredProducts.length }}<ng-container *ngIf="productSearch.trim()"> matching</ng-container><ng-container *ngIf="!productSearch.trim()"> total</ng-container></span></div>
            <div class="product-row" *ngFor="let product of filteredProducts">
              <ng-container *ngIf="editingProductId !== product.id; else editProductRow">
                <div class="product-info"><strong>{{ product.particular }}</strong><span>{{ product.defaultUnit }}<ng-container *ngIf="product.defaultRate > 0"> · ₹{{ product.defaultRate }}</ng-container></span></div>
                <div class="product-actions"><button class="btn btn-outline btn-sm" type="button" (click)="startEditProduct(product)">Edit</button><button class="btn btn-danger btn-sm" type="button" (click)="deleteProduct(product)">Delete</button></div>
              </ng-container>
              <ng-template #editProductRow>
                <div class="product-edit-fields"><input class="form-control" [name]="'editName' + product.id" [(ngModel)]="editingProduct.particular" aria-label="Product name" /><input class="form-control" [name]="'editUnit' + product.id" [(ngModel)]="editingProduct.defaultUnit" [attr.list]="'product-units'" aria-label="Unit" /><input class="form-control rate-input" [name]="'editRate' + product.id" [(ngModel)]="editingProduct.defaultRate" type="number" min="0" step="0.01" aria-label="Default rate" /></div>
                <div class="product-actions"><button class="btn btn-primary btn-sm" type="button" (click)="saveProductEdit(product.id)" [disabled]="savingProduct">Save</button><button class="btn btn-outline btn-sm" type="button" (click)="cancelEditProduct()">Cancel</button></div>
              </ng-template>
            </div>
          </div>
          <ng-template #emptyProductResults><p class="empty-products">{{ savedProducts.length ? 'No products match your search.' : 'No saved products yet. Add one above to make it available in quotation maker.' }}</p></ng-template>
          </ng-container>
        </section>

        <section class="settings-card">
          <div class="settings-card-heading">
            <div class="settings-icon green-icon">🏦</div>
            <div>
              <h2>Quotation payment details</h2>
              <p>These bank details and terms are used as the defaults on new quotations and printed on the quotation PDF.</p>
            </div>
          </div>
          <div class="settings-form-grid">
            <label class="settings-field">
              <span>Bank name</span>
              <input class="form-control" [(ngModel)]="bank.bankName" name="bankName" placeholder="e.g. State Bank of India" />
            </label>
            <label class="settings-field">
              <span>Account holder name</span>
              <input class="form-control" [(ngModel)]="bank.accountName" name="accountName" placeholder="Name on the account" />
            </label>
            <label class="settings-field">
              <span>Account number</span>
              <input class="form-control" [(ngModel)]="bank.accountNumber" name="accountNumber" autocomplete="off" inputmode="numeric" />
            </label>
            <label class="settings-field">
              <span>IFSC code</span>
              <input class="form-control" [(ngModel)]="bank.ifscCode" name="ifscCode" autocapitalize="characters" />
            </label>
            <label class="settings-field">
              <span>Branch</span>
              <input class="form-control" [(ngModel)]="bank.branch" name="branch" />
            </label>
          </div>
          <label class="settings-field terms-field">
            <span>Quotation terms &amp; conditions</span>
            <textarea class="form-control" [(ngModel)]="bank.termsAndConditions" name="termsAndConditions" rows="7" placeholder="Add one term per line"></textarea>
            <small>These terms will appear on quotations made after you save.</small>
          </label>
          <div class="settings-card-footer">
            <span class="settings-hint">Saved as the default for future quotations.</span>
            <button class="btn btn-primary" type="button" (click)="saveQuotationDefaults()" [disabled]="savingQuotationDefaults">
              {{ savingQuotationDefaults ? 'Saving…' : 'Save quotation defaults' }}
            </button>
          </div>
        </section>
      </main>
    </div>
  `,
  styles: [`
    .settings-page { max-width: 1080px; }
    .settings-heading { display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:24px; }
    .settings-eyebrow { color:#218463; font-size:.68rem; font-weight:800; letter-spacing:.1em; margin-bottom:5px; }
    .settings-heading h1 { font-size:1.8rem; line-height:1.2; letter-spacing:-.035em; color:#17231f; }
    .settings-heading p { margin-top:6px; color:#68766f; font-size:.9rem; }
    .settings-card { background:#fff; border:1px solid #e5ebe7; border-radius:14px; padding:24px; margin-bottom:18px; box-shadow:0 2px 8px rgba(20,55,39,.035); }
    .settings-card-heading { display:flex; gap:13px; align-items:flex-start; padding-bottom:20px; margin-bottom:20px; border-bottom:1px solid #edf1ee; }
    .products-toggle { width:100%; text-align:left; color:inherit; background:transparent; border:0; cursor:pointer; }
    .products-heading-copy { flex:1; }
    .products-toggle-icon { color:#718078; font-size:1.3rem; font-weight:500; line-height:1; padding:6px 10px; border:1px solid #e5ebe7; border-radius:8px; }
    .settings-icon { width:40px; height:40px; flex:0 0 40px; display:grid; place-items:center; border-radius:11px; background:#f1f4ff; font-size:1.1rem; }
    .settings-icon.green-icon { background:#edf7f2; }
    .settings-icon.product-icon { background:#fff6e8; }
    .settings-card-heading h2 { font-size:1.05rem; color:#1b2822; margin:1px 0 4px; }
    .settings-card-heading p { color:#748078; font-size:.82rem; line-height:1.5; }
    .settings-form-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:17px 20px; }
    .settings-field { display:flex; flex-direction:column; gap:7px; min-width:0; }
    .settings-field > span { font-size:.78rem; font-weight:700; color:#35433c; }
    .settings-field small { color:#829087; font-size:.72rem; font-weight:500; }
    .settings-field .form-control { width:100%; }
    .terms-field { margin-top:19px; }
    .terms-field textarea { min-height:150px; resize:vertical; line-height:1.55; }
    .terms-field > small { color:#78857d; }
    .settings-card-footer { display:flex; justify-content:space-between; align-items:center; gap:14px; border-top:1px solid #edf1ee; padding-top:18px; margin-top:20px; }
    .settings-hint { color:#7a877f; font-size:.75rem; }
    .product-create-form { display:grid; grid-template-columns:minmax(220px,1.5fr) minmax(120px,.7fr) minmax(130px,.7fr) auto; align-items:end; gap:12px; }
    .product-search { margin:0 0 16px; max-width:440px; }
    .product-create-form > .btn { min-height:40px; }
    .product-list { margin-top:22px; border:1px solid #e8eeea; border-radius:10px; overflow:hidden; }
    .product-list-heading { display:flex; justify-content:space-between; padding:10px 14px; background:#f7f9f8; color:#748078; font-size:.72rem; font-weight:700; }
    .product-row { min-height:62px; display:flex; align-items:center; justify-content:space-between; gap:14px; padding:10px 14px; border-top:1px solid #edf1ee; }
    .product-info { min-width:0; display:flex; flex-direction:column; gap:4px; }
    .product-info strong { color:#25332c; font-size:.85rem; font-weight:650; overflow-wrap:anywhere; }
    .product-info span { color:#7b8780; font-size:.73rem; }
    .product-actions { display:flex; align-items:center; gap:8px; flex-shrink:0; }
    .btn-sm { padding:6px 10px; font-size:.72rem; }
    .btn-danger { color:#b42318; border:1px solid #f1c7c2; background:#fff; }
    .btn-danger:hover { background:#fff4f2; }
    .product-edit-fields { flex:1; display:grid; grid-template-columns:minmax(120px,1fr) 100px 110px; gap:8px; }
    .product-edit-fields .form-control { min-width:0; }
    .empty-products { color:#7a877f; padding:16px 0 2px; font-size:.82rem; }
    @media(max-width:700px) { .settings-form-grid { grid-template-columns:1fr; } .settings-card { padding:18px; } .settings-card-footer { align-items:stretch; flex-direction:column; } .settings-card-footer .btn { width:100%; } .product-create-form { grid-template-columns:1fr 1fr; } .product-name-field { grid-column:1/-1; } .product-create-form > .btn { width:100%; } .product-row { align-items:flex-start; flex-direction:column; } .product-actions { width:100%; justify-content:flex-end; } .product-edit-fields { width:100%; grid-template-columns:1fr 1fr; } .product-edit-fields input:first-child { grid-column:1/-1; } }
  `]
})
export class SettingsComponent implements OnInit {
  account = { username: '', currentPassword: '', newPassword: '', confirmPassword: '' };
  bank: BankDetails = {
    bankName: '', accountName: '', accountNumber: '', ifscCode: '', branch: '',
    termsAndConditions: ''
  };
  savingAccount = false;
  savingQuotationDefaults = false;
  savedProducts: SavedParticular[] = [];
  showProducts = false;
  productSearch = '';
  availableUnits: string[] = [];
  newProduct = { particular: '', defaultUnit: 'mtr', defaultRate: 0 };
  editingProductId: number | null = null;
  editingProduct = { particular: '', defaultUnit: '', defaultRate: 0 };
  savingProduct = false;

  constructor(private api: ApiService, private auth: AuthService, private toast: ToastService) {
    this.account.username = this.auth.getUsername();
  }

  ngOnInit(): void {
    this.api.getBankDetails().subscribe({
      next: details => this.bank = { ...this.bank, ...details },
      error: () => this.toast.error('Could not load quotation payment details')
    });
    this.loadProducts();
  }

  private loadProducts(): void {
    this.api.getParticulars().subscribe({ next: products => this.savedProducts = products, error: () => this.toast.error('Could not load saved products') });
    this.api.getUnits().subscribe({ next: units => this.availableUnits = units || [] });
  }

  toggleProducts(): void { this.showProducts = !this.showProducts; }

  get filteredProducts(): SavedParticular[] {
    const query = this.productSearch.trim().toLocaleLowerCase();
    return query
      ? this.savedProducts.filter(product => product.particular.toLocaleLowerCase().includes(query))
      : this.savedProducts;
  }

  addProduct(): void {
    const data = { particular: this.newProduct.particular.trim(), defaultUnit: this.newProduct.defaultUnit.trim(), defaultRate: Number(this.newProduct.defaultRate) || 0 };
    if (!data.particular || !data.defaultUnit) return;
    if (this.savedProducts.some(product => product.particular.toLocaleLowerCase() === data.particular.toLocaleLowerCase())) {
      this.toast.error('A product with this name is already saved. Edit it from the list instead.');
      return;
    }
    this.savingProduct = true;
    this.api.saveParticular(data).subscribe({
      next: product => {
        this.savingProduct = false;
        this.savedProducts = [product, ...this.savedProducts.filter(item => item.id !== product.id)].sort((a, b) => a.particular.localeCompare(b.particular));
        this.newProduct = { particular: '', defaultUnit: 'mtr', defaultRate: 0 };
        this.toast.success('Product added');
      },
      error: err => { this.savingProduct = false; this.toast.error(err?.error || 'Could not add product'); }
    });
  }

  startEditProduct(product: SavedParticular): void {
    this.editingProductId = product.id;
    this.editingProduct = { particular: product.particular, defaultUnit: product.defaultUnit || 'mtr', defaultRate: product.defaultRate || 0 };
  }

  cancelEditProduct(): void { this.editingProductId = null; }

  saveProductEdit(id: number): void {
    const data = { particular: this.editingProduct.particular.trim(), defaultUnit: this.editingProduct.defaultUnit.trim(), defaultRate: Number(this.editingProduct.defaultRate) || 0 };
    if (!data.particular || !data.defaultUnit) { this.toast.error('Enter a product name and unit'); return; }
    this.savingProduct = true;
    this.api.updateParticular(id, data).subscribe({
      next: updated => { this.savedProducts = this.savedProducts.map(product => product.id === id ? updated : product).sort((a, b) => a.particular.localeCompare(b.particular)); this.editingProductId = null; this.savingProduct = false; this.toast.success('Product updated'); },
      error: err => { this.savingProduct = false; this.toast.error(err?.error || 'Could not update product'); }
    });
  }

  deleteProduct(product: SavedParticular): void {
    if (!confirm(`Delete “${product.particular}” from saved products?`)) return;
    this.api.deleteParticular(product.id).subscribe({
      next: () => { this.savedProducts = this.savedProducts.filter(item => item.id !== product.id); this.toast.success('Product deleted'); },
      error: () => this.toast.error('Could not delete product')
    });
  }

  saveAccount(): void {
    const username = this.account.username.trim();
    if (!username || !this.account.currentPassword) {
      this.toast.error('Enter your username and current password');
      return;
    }
    if (this.account.newPassword && this.account.newPassword.length < 8) {
      this.toast.error('New password must be at least 8 characters');
      return;
    }
    if (this.account.newPassword !== this.account.confirmPassword) {
      this.toast.error('The new passwords do not match');
      return;
    }

    this.savingAccount = true;
    this.auth.updateAccount({
      currentPassword: this.account.currentPassword,
      username,
      newPassword: this.account.newPassword || undefined
    }).subscribe({
      next: () => {
        this.account.username = username;
        this.account.currentPassword = '';
        this.account.newPassword = '';
        this.account.confirmPassword = '';
        this.savingAccount = false;
        this.toast.success('Account settings updated');
      },
      error: err => {
        this.savingAccount = false;
        this.toast.error(err?.error?.message || 'Could not update account settings');
      }
    });
  }

  saveQuotationDefaults(): void {
    this.savingQuotationDefaults = true;
    this.api.saveBankDetails(this.bank).subscribe({
      next: details => {
        this.bank = details;
        this.savingQuotationDefaults = false;
        this.toast.success('Quotation payment details and terms saved');
      },
      error: () => {
        this.savingQuotationDefaults = false;
        this.toast.error('Could not save quotation details');
      }
    });
  }
}
