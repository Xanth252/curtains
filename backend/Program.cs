using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using backend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=pfc.db"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""QuotationWindows"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_QuotationWindows"" PRIMARY KEY AUTOINCREMENT,
                ""QuotationId"" INTEGER NOT NULL,
                ""Location"" TEXT NOT NULL,
                ""TreatmentType"" TEXT NOT NULL,
                ""Width"" REAL NOT NULL,
                ""Height"" REAL NOT NULL,
                ""Panels"" INTEGER NULL,
                ""Requirement"" REAL NOT NULL,
                ""Unit"" TEXT NOT NULL,
                ""Remarks"" TEXT NULL,
                CONSTRAINT ""FK_QuotationWindows_Quotations_QuotationId"" FOREIGN KEY (""QuotationId"") REFERENCES ""Quotations"" (""Id"") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ""IX_QuotationWindows_QuotationId"" ON ""QuotationWindows"" (""QuotationId"");
        ");
    }
    catch { /* Table already exists or handled */ }

    try
    {
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Quotations"" ADD COLUMN ""QuotationNumber"" TEXT DEFAULT ''");
    }
    catch { /* Column already exists */ }

    try
    {
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""QuotationWindows"" ADD COLUMN ""WindowName"" TEXT DEFAULT ''");
    }
    catch { /* Column already exists */ }

    try
    {
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""QuotationWindows"" ADD COLUMN ""CurtainType"" TEXT DEFAULT 'Main'");
    }
    catch { /* Column already exists */ }

    // Keep databases created before quotation discount fields were introduced compatible.
    var quotationColumnUpgrades = new[]
    {
        "ALTER TABLE \"Quotations\" ADD COLUMN \"DiscountType\" TEXT NOT NULL DEFAULT 'percent'",
        "ALTER TABLE \"Quotations\" ADD COLUMN \"DiscountValue\" decimal NOT NULL DEFAULT 0",
        "ALTER TABLE \"Quotations\" ADD COLUMN \"DiscountAmount\" decimal NOT NULL DEFAULT 0",
        "ALTER TABLE \"Quotations\" ADD COLUMN \"SubTotal\" decimal NOT NULL DEFAULT 0",
        "ALTER TABLE \"Quotations\" ADD COLUMN \"FinalAmount\" decimal NOT NULL DEFAULT 0",
        "ALTER TABLE \"Quotations\" ADD COLUMN \"QuotationStatus\" TEXT NOT NULL DEFAULT 'Pending'"
        ,"ALTER TABLE \"Quotations\" ADD COLUMN \"PaymentStatus\" TEXT NOT NULL DEFAULT 'Pending'"
    };

    foreach (var upgrade in quotationColumnUpgrades)
    {
        try
        {
            db.Database.ExecuteSqlRaw(upgrade);
        }
        catch { /* Column already exists */ }
    }

    var quotationItemColumnUpgrades = new[]
    {
        "ALTER TABLE \"QuotationItems\" ADD COLUMN \"Description\" TEXT NULL"
    };

    foreach (var upgrade in quotationItemColumnUpgrades)
    {
        try
        {
            db.Database.ExecuteSqlRaw(upgrade);
        }
        catch { /* Column already exists */ }
    }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""QuotationItems"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_QuotationItems"" PRIMARY KEY AUTOINCREMENT,
                ""QuotationId"" INTEGER NOT NULL,
                ""SrNo"" INTEGER NOT NULL DEFAULT 1,
                ""Particular"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""Quantity"" REAL NOT NULL DEFAULT 1,
                ""Unit"" TEXT NOT NULL DEFAULT 'mtr',
                ""Rate"" decimal NOT NULL DEFAULT 0,
                ""Total"" decimal NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_QuotationItems_Quotations_QuotationId"" FOREIGN KEY (""QuotationId"") REFERENCES ""Quotations"" (""Id"") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ""IX_QuotationItems_QuotationId"" ON ""QuotationItems"" (""QuotationId"");
        ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""SavedParticulars"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SavedParticulars"" PRIMARY KEY AUTOINCREMENT,
                ""Particular"" TEXT NOT NULL,
                ""DefaultUnit"" TEXT NOT NULL DEFAULT 'mtr',
                ""DefaultRate"" decimal NOT NULL DEFAULT 0,
                ""UsageCount"" INTEGER NOT NULL DEFAULT 1,
                ""LastUsedAt"" TEXT NOT NULL
            );
        ");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY AUTOINCREMENT,
                ""Key"" TEXT NOT NULL,
                ""Value"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NOT NULL
            );
        ");
    }
    catch { }

    // Seed default particulars if empty
    if (!db.SavedParticulars.Any())
    {
        var defaults = new List<backend.Models.SavedParticular>
        {
            new() { Particular = "Curtain Main Fabric", DefaultUnit = "mtr", DefaultRate = 850, UsageCount = 10, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Curtain Sheer / Net Fabric", DefaultUnit = "mtr", DefaultRate = 450, UsageCount = 8, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Heavy Duty Aluminium Track Channel", DefaultUnit = "ft", DefaultRate = 120, UsageCount = 7, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Curtain Stitching & Eyelet Finishing", DefaultUnit = "mtr", DefaultRate = 180, UsageCount = 6, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Roman Blinds with Fabric & Mechanism", DefaultUnit = "sq ft", DefaultRate = 220, UsageCount = 5, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Zebra / Roller Blinds", DefaultUnit = "sq ft", DefaultRate = 160, UsageCount = 4, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Designer Wallpaper Supply & Installation", DefaultUnit = "sq ft", DefaultRate = 75, UsageCount = 4, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Curtain Tiebacks & Brass Hooks", DefaultUnit = "pair", DefaultRate = 350, UsageCount = 3, LastUsedAt = DateTime.UtcNow },
            new() { Particular = "Installation & Fitting Charges", DefaultUnit = "pcs", DefaultRate = 150, UsageCount = 3, LastUsedAt = DateTime.UtcNow }
        };
        db.SavedParticulars.AddRange(defaults);
        db.SaveChanges();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
