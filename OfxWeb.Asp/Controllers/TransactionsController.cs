using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using Microsoft.AspNetCore.Http;
using OfxSharpLib;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;

namespace OfxWeb.Asp.Controllers
{
    [Authorize(Roles = "Verified")]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int pagesize = 100;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transactions
        public async Task<IActionResult> Index(string sortOrder, string searchString, string searchCategory, int? page)
        {
            // Sort/Filter: https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/sort-filter-page?view=aspnetcore-2.1

            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "date_desc";

            ViewData["DateSortParm"] = sortOrder == "date_desc" ? "date_asc" : "date_desc";
            ViewData["PayeeSortParm"] = sortOrder == "payee_asc" ? "payee_desc" : "payee_asc";
            ViewData["CategorySortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["AmountSortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["BankReferenceSortParm"] = sortOrder == "ref_asc" ? "ref_desc" : "ref_asc";
            ViewData["CurrentFilter"] = searchString;

            if (!page.HasValue)
                page = 1;

            var result = from s in _context.Transactions
                         select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                result = result.Where(x => x.Payee.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(searchCategory))
            {
                result = result.Where(x => x.Category.Contains(searchCategory));
            }

            switch (sortOrder)
            {
                case "amount_asc":
                    result = result.OrderBy(s => s.Amount);
                    break;
                case "amount_desc":
                    result = result.OrderByDescending(s => s.Amount);
                    break;
                case "ref_asc":
                    result = result.OrderBy(s => s.BankReference);
                    break;
                case "ref_desc":
                    result = result.OrderByDescending(s => s.BankReference);
                    break;
                case "payee_asc":
                    result = result.OrderBy(s => s.Payee);
                    break;
                case "payee_desc":
                    result = result.OrderByDescending(s => s.Payee);
                    break;
                case "category_asc":
                    result = result.OrderBy(s => s.Category);
                    break;
                case "category_desc":
                    result = result.OrderByDescending(s => s.Category);
                    break;
                case "date_asc":
                    result = result.OrderBy(s => s.Timestamp).ThenBy(s=>s.BankReference);
                    break;
                case "date_desc":
                default:
                    result = result.OrderByDescending(s => s.Timestamp).ThenByDescending(s=>s.BankReference);
                    break;
            }

            var count = await result.CountAsync();

            if (count > pagesize)
            {
                result = result.Skip((page.Value - 1) * pagesize).Take(pagesize);

                if (page.Value > 1)
                    ViewData["PreviousPage"] = page.Value - 1;
                if (page * pagesize < count)
                    ViewData["NextPage"] = page.Value + 1;

                ViewData["CurrentSort"] = sortOrder;
            }

            return View(await result.AsNoTracking().ToListAsync());
        }

        // GET: Transactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transactions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.Include(x=>x.Splits).SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Handle payee auto-assignment

            if (string.IsNullOrEmpty(transaction.Category) && string.IsNullOrEmpty(transaction.SubCategory))
            {
                // See if the payee exists
                var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

                if (payee != null)
                {
                    transaction.Category = payee.Category;
                    transaction.SubCategory = payee.SubCategory;
                    ViewData["AutoCategory"] = true;
                }
            }

            return View(transaction);
        }

        // GET: Transactions/ApplyPayee/5
        public async Task<IActionResult> ApplyPayee(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Handle payee auto-assignment

            // See if the payee exists
            var payee = await _context.Payees.FirstOrDefaultAsync(x => transaction.Payee.Contains(x.Name));

            if (payee != null)
            {
                transaction.Category = payee.Category;
                transaction.SubCategory = payee.SubCategory;
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,SubCategory,BankReference")] Models.Transaction transaction)
        {
            if (id != transaction.ID && duplicate != true)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (duplicate == true)
                    {
                        transaction.ID = 0;
                        _context.Add(transaction);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _context.Update(transaction);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .SingleOrDefaultAsync(m => m.ID == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(m => m.ID == id);
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var incoming = new HashSet<Models.Transaction>(new TransactionBankReferenceComparer());
            try
            {

                // Build the submitted file into a list of transactions

                foreach (var formFile in files)
                {
                    if (formFile.FileName.ToLower().EndsWith(".ofx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var parser = new OfxDocumentParser();
                            var Document = parser.Import(stream);

                            await Task.Run(() =>
                            {
                                foreach (var tx in Document.Transactions)
                                {
                                    incoming.Add(new Models.Transaction() { Amount = tx.Amount, Payee = tx.Memo.Trim(), BankReference = tx.ReferenceNumber.Trim(), Timestamp = tx.Date });
                                }
                            });
                        }
                    }
                    else
                    if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                    {
                        using (var stream = formFile.OpenReadStream())
                        {
                            var excel = new ExcelPackage(stream);
                            var worksheet = excel.Workbook.Worksheets.Where(x=>x.Name == "Transactions").Single();
                            worksheet.ExtractInto(incoming);
                        }
                    }
                }

                // Remove duplicate transactions.

                var existing = await _context.Transactions.Where(x => incoming.Contains(x)).ToListAsync();
                incoming.ExceptWith(existing);

                // Add resulting transactions

                await _context.AddRangeAsync(incoming);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

            return View(incoming.OrderByDescending(x => x.Timestamp));
        }

        // GET: Transactions/Pivot
        public IActionResult Pivot(string report)
        {
            PivotTable<Label, Label, decimal> result = null;

            if (string.IsNullOrEmpty(report))
            {
                report = "monthly";
            }

            switch (report)
            {
                case "yearly":
                    result = ThreeLevelReport(YearlyCategories);
                    break;

                case "details":
                    result = ThreeLevelReport(DetailCategories);
                    break;

                case "budgettx":
                    result = BudgetTxReport();
                    break;

                case "monthly":
                default:
                    result = MonthlyReport();
                    break;
            }

            return View(result);
        }

        private string[] YearlyCategories = new[] { "RV", "Yearly", "Travel", "Transfer", "Medical", "App Development", "Yearly.Housing", "Yearly.James", "Yearly.Sheila", "Yearly.Auto & Transport", "Yearly.Entertainment", "Yearly.Kids", "Yearly.Shopping" };

        private string[] DetailCategories = new[] { "Auto & Transport", "Groceries", "Utilities" };

        private PivotTable<Label, Label, decimal> ThreeLevelReport(ICollection<string> categories)
        {
            var result = new PivotTable<Label, Label, decimal>();

            // This crazy report is THREE levels of grouping!! Months for columns, then rows and subrows for
            // categories and subcategories

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL", Emphasis = true };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };

            var outergroups = _context.Transactions.Where(x => x.Timestamp.Year == 2018).Where(x => categories.Contains(x.Category)).GroupBy(x => x.Timestamp.Month);

            if (outergroups != null)
                foreach (var outergroup in outergroups)
                {
                    var month = outergroup.Key;
                    var labelcol = new Label() { Order = month, Value = new DateTime(2018, month, 1).ToString("MMM") };

                    if (outergroup.Count() > 0)
                    {
                        decimal outersum = 0.0M;
                        var innergroups = outergroup.GroupBy(x => x.Category);

                        foreach (var innergroup in innergroups)
                        {
                            var sum = innergroup.Sum(x => x.Amount);

                            var labelrow = labelempty;
                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                labelrow = new Label() { Order = 0, Value = innergroup.Key, Emphasis = true };
                            }

                            result[labelcol, labelrow] = sum;
                            outersum += sum;

                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                var subgroups = innergroup.GroupBy(x => x.SubCategory);

                                foreach (var subgroup in subgroups)
                                {
                                    sum = subgroup.Sum(x => x.Amount);
                                    labelrow = new Label() { Order = 0, Value = innergroup.Key, SubValue = subgroup.Key ?? "-" };
                                    result[labelcol, labelrow] = sum;
                                }
                            }
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            foreach (var row in result.Table)
            {
                var rowsum = row.Value.Values.Sum();
                result[labeltotal, row.Key] = rowsum;
            }

            return result;
        }

        private PivotTable<Label, Label, decimal> MonthlyReport()
        {
            var result = new PivotTable<Label, Label, decimal>();

            // Create a grouping of results.
            //
            // For this one we want rows: Category, cols: Month, filter: Year

            // This is not working :(
            // https://docs.microsoft.com/en-us/dotnet/csharp/linq/create-a-nested-group
            /*
            var qq = from tx in _context.Transactions
                     where tx.Timestamp.Year == 2018 && ! string.IsNullOrEmpty(tx.Category)
                     group tx by tx.Timestamp.Month into months
                     from categories in (from tx in months group tx by tx.Category)
                     group categories by months.Key;
            */

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL" };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };

            var outergroups = _context.Transactions.Where(x => x.Timestamp.Year == 2018 && ( ! YearlyCategories.Contains(x.Category) || x.Category == null )).GroupBy(x => x.Timestamp.Month);

            if (outergroups != null)
                foreach (var outergroup in outergroups)
                {
                    var month = outergroup.Key;
                    var labelcol = new Label() { Order = month, Value = new DateTime(2018, month, 1).ToString("MMM") };

                    if (outergroup.Count() > 0)
                    {
                        decimal outersum = 0.0M;
                        var innergroups = outergroup.GroupBy(x => x.Category);

                        foreach (var innergroup in innergroups)
                        {
                            var sum = innergroup.Sum(x => x.Amount);

                            var labelrow = labelempty;
                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                labelrow = new Label() { Order = 0, Value = innergroup.Key };
                            }

                            result[labelcol, labelrow] = sum;
                            outersum += sum;
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            foreach (var row in result.Table)
            {
                var rowsum = row.Value.Values.Sum();
                result[labeltotal, row.Key] = rowsum;
            }

            return result;
        }

        private PivotTable<Label, Label, decimal> BudgetTxReport()
        {
            var result = new PivotTable<Label, Label, decimal>();

            // Create a grouping of results.
            //
            // For this one we want rows: Category, cols: Month, filter: Year

            // This is not working :(
            // https://docs.microsoft.com/en-us/dotnet/csharp/linq/create-a-nested-group
            /*
            var qq = from tx in _context.Transactions
                     where tx.Timestamp.Year == 2018 && ! string.IsNullOrEmpty(tx.Category)
                     group tx by tx.Timestamp.Month into months
                     from categories in (from tx in months group tx by tx.Category)
                     group categories by months.Key;
            */

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL" };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };

            var outergroups = _context.BudgetTxs.Where(x => x.Timestamp.Year == 2018).GroupBy(x => x.Timestamp.Month);

            if (outergroups != null)
                foreach (var outergroup in outergroups)
                {
                    var month = outergroup.Key;
                    var labelcol = new Label() { Order = month, Value = new DateTime(2018, month, 1).ToString("MMM") };

                    if (outergroup.Count() > 0)
                    {
                        decimal outersum = 0.0M;
                        var innergroups = outergroup.GroupBy(x => x.Category);

                        foreach (var innergroup in innergroups)
                        {
                            var sum = innergroup.Sum(x => x.Amount);

                            var labelrow = labelempty;
                            if (!string.IsNullOrEmpty(innergroup.Key))
                            {
                                labelrow = new Label() { Order = 0, Value = innergroup.Key };
                            }

                            result[labelcol, labelrow] = sum;
                            outersum += sum;
                        }
                        result[labelcol, labeltotal] = outersum;
                    }
                }

            foreach (var row in result.Table)
            {
                var rowsum = row.Value.Values.Sum();
                result[labeltotal, row.Key] = rowsum;
            }

            return result;
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.ID == id);
        }
    }

    class TransactionBankReferenceComparer : IEqualityComparer<Models.Transaction>
    {
        public bool Equals(Models.Transaction x, Models.Transaction y)
        {
            return x.BankReference == y.BankReference;
        }

        public int GetHashCode(Models.Transaction obj)
        {
            int result;
            if (!int.TryParse(obj.BankReference,out result))
            {
                result = obj.BankReference.GetHashCode();
            }
            return result;
        }
    }

    public class SparseDictionary<K,V>: Dictionary<K,V>
    {
        public new V this[K key]
        {
            get
            {
                return base.ContainsKey(key) ? base[key] : default(V);
            }
            set
            {
                base[key] = value;
            }
        }
    }

    public class Label: IComparable<Label>
    {
        public int Order { get; set; }
        public string Value { get; set; } = string.Empty;
        public string SubValue { get; set; } = string.Empty;
        public bool Emphasis { get; set; } = false;

        public int CompareTo(Label other)
        {
            if (Order == 0 && other.Order == 0)
            {
                if (Value == other.Value)
                    return (SubValue ?? String.Empty).CompareTo(other.SubValue ?? string.Empty);
                else
                    return Value.CompareTo(other.Value);
            }
            else
                return Order.CompareTo(other.Order);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Label;

            if (other.Order == 0 && Order == 0)
                return Value.Equals(other.Value) && (SubValue ?? String.Empty).Equals(other.SubValue ?? String.Empty) == true;
            else
                return Order.Equals(other.Order);
        }

        public override int GetHashCode()
        {
            return Order == 0 ? Value.GetHashCode() : Order;
        }
    }

    public class PivotTable<C,R,V>
    {
        // First order keys is row (category), second order is column (month)
        public Dictionary<R, SparseDictionary<C, V>> Table = new Dictionary<R, SparseDictionary<C, V>>();

        public HashSet<C> Columns = new HashSet<C>();

        public IEnumerable<R> RowLabels => Table.Keys.OrderBy(x => x);

        public V this[C collabel, R rowlabel]
        {
            get
            {
                if (Table.ContainsKey(rowlabel))
                {
                    return Table[rowlabel][collabel];
                }
                else
                {
                    return default(V);
                }
            }
            set
            {
                if (!Table.ContainsKey(rowlabel))
                {
                    Table[rowlabel] = new SparseDictionary<C, V>();
                }
                var row = Table[rowlabel];

                row[collabel] = value;

                Columns.Add(collabel);
            }
        }
    }
}
