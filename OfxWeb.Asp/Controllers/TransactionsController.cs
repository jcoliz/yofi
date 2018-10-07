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
using OfficeOpenXml.Table;

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
        public async Task<IActionResult> Index(string sortOrder, string search, string searchPayee, string searchCategory, int? page)
        {
            // Sort/Filter: https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/sort-filter-page?view=aspnetcore-2.1

            if (string.IsNullOrEmpty(sortOrder))
                sortOrder = "date_desc";

            ViewData["DateSortParm"] = sortOrder == "date_desc" ? "date_asc" : "date_desc";
            ViewData["PayeeSortParm"] = sortOrder == "payee_asc" ? "payee_desc" : "payee_asc";
            ViewData["CategorySortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["AmountSortParm"] = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            ViewData["BankReferenceSortParm"] = sortOrder == "ref_asc" ? "ref_desc" : "ref_asc";

            bool showHidden = false;

            // 'search' parameter combines all search types
            if (!String.IsNullOrEmpty(search))
            {
                var terms = search.Split(',');
                foreach(var term in terms)
                {
                    if (term[0] == 'P')
                    {
                        searchPayee = term.Substring(2);
                    }
                    else if (term[0] == 'C')
                    {
                        searchCategory = term.Substring(2);
                    }
                    else if (term[0] == 'H')
                    {
                        showHidden = true;
                    }
                }
            }

            ViewData["ShowHidden"] = showHidden;
            ViewData["CurrentSearchPayee"] = searchPayee;
            ViewData["CurrentSearchCategory"] = searchCategory;

            var searchlist = new List<string>();
            if (!String.IsNullOrEmpty(searchPayee))
                searchlist.Add($"P-{searchPayee}");
            if (!String.IsNullOrEmpty(searchCategory))
                searchlist.Add($"C-{searchCategory}");

            if (showHidden)
            {
                ViewData["CurrentFilterToggleHidden"] = string.Join(',', searchlist);
                searchlist.Add("H");
                ViewData["CurrentFilter"] = string.Join(',', searchlist);
            }
            else
            {
                ViewData["CurrentFilter"] = string.Join(',', searchlist);
                searchlist.Add("H");
                ViewData["CurrentFilterToggleHidden"] = string.Join(',', searchlist);
            }

            if (!page.HasValue)
                page = 1;

            var result = from s in _context.Transactions
                         select s;

            if (!String.IsNullOrEmpty(searchPayee))
            {
                result = result.Where(x => x.Payee.Contains(searchPayee));
            }

            if (!String.IsNullOrEmpty(searchCategory))
            {
                if (searchCategory == "-")
                    result = result.Where(x => string.IsNullOrEmpty(x.Category));
                else
                    result = result.Where(x => x.Category.Contains(searchCategory) || x.SubCategory.Contains(searchCategory));
            }

            if (!showHidden)
            {
                result = result.Where(x => x.Hidden != true);
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
            var incoming = new HashSet<Models.Transaction>();
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

                var uniqueids = incoming.Select(x => x.BankReference).ToHashSet();

                var existing = await _context.Transactions.Where(x => uniqueids.Contains(x.BankReference)).ToListAsync();
                incoming.ExceptWith(existing);

                // Fix up the remaining payees

                foreach (var item in incoming)
                    item.FixupPayee();

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

        // GET: Transactions/Download
        [ActionName("Download")]
        public async Task<IActionResult> Download()
        {
            const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            try
            {
                var objecttype = "Transactions";
                var transactions = await _context.Transactions.Where(x=>x.Timestamp.Year == 2018).OrderByDescending(x => x.Timestamp).ToListAsync();

                byte[] reportBytes;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Properties.Title = objecttype;
                    package.Workbook.Properties.Author = "coliz.com";
                    package.Workbook.Properties.Subject = objecttype;
                    package.Workbook.Properties.Keywords = objecttype;

                    var worksheet = package.Workbook.Worksheets.Add(objecttype);
                    int rows, cols;
                    worksheet.PopulateFrom(transactions, out rows, out cols);

                    var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), objecttype);
                    tbl.ShowHeader = true;
                    tbl.TableStyle = TableStyles.Dark9;

                    reportBytes = package.GetAsByteArray();
                }

                return File(reportBytes, XlsxContentType, $"{objecttype}.xlsx");
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        // GET: Transactions/Pivot
        public IActionResult Pivot(string report,int? month, int? weekspct)
        {
            PivotTable<Label, Label, decimal> result = null;
            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;
            IEnumerable<IGrouping<int, ISubReportable>> groupsL2 = null;

            if (string.IsNullOrEmpty(report))
            {
                report = "monthly";
            }

            if (!month.HasValue)
            {
                month = DateTime.Now.Month;

                // By default, budgetmo goes through LAST month, unless it's January
                if (report == "budgetmo" && month > 1)
                    --month;
            }

            var period = new DateTime(DateTime.Now.Year, month.Value, 1);

            ViewData["Subtitle"] = $"For {DateTime.Now.Year} year to date";

            switch (report)
            {
                case "yearly":
                    groupsL2 = _context.Transactions.Where(x => x.Timestamp.Year == 2018).Where(x => YearlyCategories.Contains(x.Category) && x.Hidden != true).GroupBy(x => x.Timestamp.Month);
                    result = ThreeLevelReport(groupsL2);
                    ViewData["Title"] = "Yearly Report";
                    break;

                case "details":
                    groupsL2 = _context.Transactions.Where(x => x.Timestamp.Year == 2018).Where(x => DetailCategories.Contains(x.Category) && x.Hidden != true).GroupBy(x => x.Timestamp.Month);
                    result = ThreeLevelReport(groupsL2);
                    ViewData["Title"] = "Transaction Details Report";
                    break;

                case "budgettx":
                    groupsL1 = _context.BudgetTxs.Where(x => x.Timestamp.Year == 2018).GroupBy(x => x.Timestamp.Month);
                    result = TwoLevelReport(groupsL1);
                    ViewData["Title"] = "Budget Transaction Report";
                    break;

                case "budgetmo":
                    result = BudgetMonthlyReport(month.Value);
                    ViewData["Title"] = "Monthly Budget Report";
                    ViewData["Subtitle"] = $"For {DateTime.Now.Year} through {period.ToString("MMMM yyyy")} ";
                    break;

                case "budget":
                    var labelcol = new Label() { Order = month.Value, Value = new DateTime(2018, month.Value, 1).ToString("MMM") };
                    result = BudgetReport(labelcol,weekspct);
                    ViewData["Title"] = "Budget vs Actuals Report";
                    ViewData["Subtitle"] = $"For {period.ToString("MMM yyyy")}";
                    if (weekspct.HasValue)
                        ViewData["Subtitle"] += $" ({weekspct.Value}%)";
                    break;

                case "monthly":
                default:
                    groupsL1 = _context.Transactions.Where(x => x.Timestamp.Year == 2018 && (!YearlyCategories.Contains(x.Category) || x.Category == null) && x.Hidden != true).GroupBy(x => x.Timestamp.Month);
                    result = TwoLevelReport(groupsL1);
                    ViewData["Title"] = "Monthly Report";
                    break;
            }

            return View(result);
        }

        private string[] YearlyCategories = new[] { "RV", "Yearly", "Travel", "Transfer", "Medical", "App Development", "Yearly.Housing", "Yearly.James", "Yearly.Sheila", "Yearly.Auto & Transport", "Yearly.Entertainment", "Yearly.Kids", "Yearly.Shopping" };

        private string[] DetailCategories = new[] { "Auto & Transport", "Groceries", "Utilities" };

        private string[] BudgetFocusCategories = new[] { "Entertainment", "Food & Dining", "Groceries", "Kids", "Shopping" };

        private PivotTable<Label, Label, decimal> BudgetReport(Label month, int? weekspct = null)
        {
            var result = new PivotTable<Label, Label, decimal>();

            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;

            groupsL1 = _context.BudgetTxs.Where(x => x.Timestamp.Year == 2018).GroupBy(x => x.Timestamp.Month);
            var budgettx = TwoLevelReport(groupsL1);

            groupsL1 = _context.Transactions.Where(x => x.Timestamp.Year == 2018 && BudgetFocusCategories.Contains(x.Category) ).GroupBy(x => x.Timestamp.Month);
            var monthlth = TwoLevelReport(groupsL1);

            Label spentLabel = new Label() { Order = 1, Value = "Spent", Format = "C0" };
            Label budgetLabel = new Label() { Order = 2, Value = "Budget", Format = "C0" };
            Label remainingLabel = new Label() { Order = 3, Value = "Remaining", Format = "C0" };
            Label pctSpentLabel = new Label() { Order = 4, Value = "% Spent", Format = "P0" };
            Label pctStatusLabel = new Label() { Order = 5, Value = "Status", Format = "P-warning" };

            foreach (var category in BudgetFocusCategories)
            {
                var labelrow = new Label() { Order = 0, Value = category };

                if (monthlth.Table.ContainsKey(labelrow))
                {
                    var budgetval = - budgettx[month, labelrow];
                    var spentval = - monthlth[month, labelrow];
                    var remaining = budgetval - spentval;

                    result[spentLabel, labelrow] = spentval;
                    result[budgetLabel, labelrow] = budgetval;
                    result[remainingLabel, labelrow] = remaining;

                    if (budgetval > 0)
                    {
                        var pct = spentval / budgetval;
                        result[pctSpentLabel, labelrow] = pct;

                        if (weekspct.HasValue)
                        {
                            var pctStatus = pct / (weekspct.Value / 100.0M);
                            result[pctStatusLabel, labelrow] = pctStatus;
                        }
                    }
                }
            }

            return result;
        }

        private PivotTable<Label, Label, decimal> BudgetMonthlyReport(int monththrough)
        {
            var result = new PivotTable<Label, Label, decimal>();

            IEnumerable<IGrouping<int, IReportable>> groupsL1 = null;

            var budgettxquery = _context.BudgetTxs.Where(x => x.Timestamp.Year == 2018 && x.Timestamp.Month <= monththrough);
            groupsL1 = budgettxquery.GroupBy(x => x.Timestamp.Month);
            var budgettx = TwoLevelReport(groupsL1);

            var categories = budgettxquery.Select(x => x.Category).Distinct().ToHashSet();
            groupsL1 = _context.Transactions.Where(x => x.Timestamp.Year == 2018 && x.Timestamp.Month <= monththrough && categories.Contains(x.Category)).GroupBy(x => x.Timestamp.Month);
            var monthlth = TwoLevelReport(groupsL1);

            foreach (var row in budgettx.RowLabels)
            {
                var labelrowbudget = new Label() { Order = row.Order + (row.Order > 0 ? 2 : 0), Value = row.Value, SubValue = "Budget" };
                var labelrowactual = new Label() { Order = row.Order + (row.Order > 0 ? 1 : 0), Value = row.Value, SubValue = "Actual" };
                var labelrow = new Label() { Order = row.Order, Value = row.Value, Emphasis = true };

                foreach(var column in budgettx.Columns)
                {
                    var budgetval = budgettx[column, labelrow];
                    var spentval = monthlth[column, labelrow];
                    var remaining = spentval - budgetval;

                    result[column, labelrow] = remaining;
                    result[column, labelrowbudget] = budgetval;
                    result[column, labelrowactual] = spentval;
                }
            }

            return result;
        }

        private PivotTable<Label, Label, decimal> ThreeLevelReport(IEnumerable<IGrouping<int, ISubReportable>> outergroups)
        {
            var result = new PivotTable<Label, Label, decimal>();

            // This crazy report is THREE levels of grouping!! Months for columns, then rows and subrows for
            // categories and subcategories

            var labeltotal = new Label() { Order = 10000, Value = "TOTAL", Emphasis = true };
            var labelempty = new Label() { Order = 9999, Value = "Blank" };

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

        private PivotTable<Label, Label, decimal> TwoLevelReport(IEnumerable<IGrouping<int,IReportable>> outergroups)
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
        public string Format { get; set; } = null;

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
