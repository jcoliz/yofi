# Budget Reports Architecture Analysis
## Keep vs. Rethink for Complete Rewrite

**Document Version:** 1.0  
**Date:** 2025-12-23  
**Author:** System Architect  
**Context:** YoFi Budget Reports V2 → V3 Rewrite Planning

---

## Executive Summary

The current budget reports architecture (V2) is fundamentally sound, with excellent abstractions and design patterns. This analysis recommends keeping approximately **80% of the core architecture** while refactoring **20%** for clarity, testability, and performance. The main improvements focus on making data flow more explicit, using stronger typing, and adding missing features rather than fundamental architectural changes.

**Key Recommendation:** This is a refinement project, not a revolution.

---

## Table of Contents

1. [Keep - Excellent Design (80%)](#keep---excellent-design-80)
2. [Rethink - Needs Improvement (20%)](#rethink---needs-improvement-20)
3. [Add - Missing Features](#add---missing-features)
4. [Implementation Roadmap](#implementation-roadmap)
5. [Migration Strategy](#migration-strategy)

---

## Keep - Excellent Design (80%)

These architectural decisions should be preserved in any rewrite. They represent solid design that has proven effective.

### 1. IReportable Interface Pattern ⭐⭐⭐⭐⭐

**Current Implementation:**
```csharp
interface IReportable
{
    decimal Amount { get; }
    DateTime Timestamp { get; }
    string Category { get; }
}

// Implementations:
// - Transaction
// - Split
// - BudgetTx
// - BudgetTx.Reportable
```

**Why Keep:**
- **Brilliant abstraction** that unifies transactions, splits, and budget items under one interface
- Makes report engine completely **data-source agnostic**
- Enables powerful composition (mixing actual + budget data seamlessly)
- Extensible for future data sources without changing report logic
- Clean, simple interface with only essential properties

**Benefits Realized:**
- Report building code doesn't care whether data comes from transactions, splits, or budgets
- Same algorithms work across all data types
- Easy to test with mock implementations
- Natural fit for LINQ operations

**Verdict:** ✅ **KEEP EXACTLY AS IS**

---

### 2. Hierarchical Category System (Colon-Separated) ⭐⭐⭐⭐

**Current Implementation:**

Categories stored and displayed identically (NO transformation):
```
Income:Salary
Housing:Mortgage
Housing:Utilities:Electric
Food:Groceries
Taxes:Federal
Savings:Retirement
[Blank]              ← Uncategorized
```

Reports filter which categories to show based on scope:
- **"All" reports**: Show everything (Income, Housing, Food, Taxes, Savings)
- **"Expenses" reports**: Exclude special categories (Income, Taxes, Savings, Transfer, Unmapped)
- **"Income" reports**: Include only Income and its children

**Why Keep:**
- **Simple string-based hierarchy** that's easy to understand and reason about
- **No transformation** - categories stored and displayed identically
- **Filtering not renaming** - report scope achieved through inclusion/exclusion
- No complex parent/child ID relationships or foreign keys
- Unlimited depth without schema changes
- Easy to parse: `category.Split(':')`
- Natural sorting and grouping behavior
- Flexible and forgiving (typos don't break database integrity)
- Human-readable in database and logs

**Alternatives Considered & Rejected:**
- ❌ **Adjacency List** (parent_id FK): Requires complex recursive queries, harder to understand
- ❌ **Materialized Path with integers**: Less human-readable, harder to debug
- ❌ **Nested Sets**: Overly complex for this use case, hard to maintain

**Benefits Realized:**
- Users can easily understand and edit categories
- Simple string operations for hierarchy manipulation
- Database queries remain straightforward
- Export/import is trivial (just strings)

**Verdict:** ✅ **KEEP - "Simple beats clever" proven right**

---

### 3. BudgetTx.Reportables Property ⭐⭐⭐⭐⭐

**Current Implementation:**
```csharp
class BudgetTx
{
    public decimal Amount { get; set; }
    public int Frequency { get; set; }  // 1=Yearly, 12=Monthly, 52=Weekly
    public DateTime Timestamp { get; set; }
    public string Category { get; set; }
    
    // Genius property:
    public IEnumerable<IReportable> Reportables
    {
        get
        {
            for (int i = 0; i < Frequency; i++)
            {
                yield return new Reportable
                {
                    Amount = Amount / Frequency,
                    Timestamp = Timestamp.AddMonths(i),  // or weeks
                    Category = Category
                };
            }
        }
    }
}
```

**Why Keep:**
- **Genius encapsulation** of frequency conversion logic
- Monthly budget of $1,200 naturally becomes 12 reportables of $100 each
- Clean separation between "what user entered" vs "what gets reported"
- Lazy evaluation (doesn't create objects until needed)
- Makes report building code completely unaware of frequency complexity

**Benefits Realized:**
- Report building code treats all reportables identically
- Frequency logic lives in exactly one place
- Easy to add new frequencies (quarterly, bi-weekly, etc.)
- Natural fit for LINQ: `budgetTxs.SelectMany(b => b.Reportables)`

**Verdict:** ✅ **KEEP - This is excellent domain modeling**

---

### 4. Declarative ReportDefinition Pattern ⭐⭐⭐⭐

**Current Implementation:**
```csharp
class ReportDefinition
{
    public string slug { get; set; }
    public string Name { get; set; }
    public string Source { get; set; }  // "Actual", "Budget", "ActualVsBudget"
    public string SourceParameters { get; set; }  // "excluded=Savings,Taxes"
    public bool WholeYear { get; set; }
    public int NumLevels { get; set; }
    public string CustomColumns { get; set; }  // "budgetpct,budgetavailable"
    public string SortOrder { get; set; }
    // ... etc
}

// Stored as static list or configuration
var definitions = new[]
{
    new ReportDefinition
    {
        slug = "expenses-v-budget",
        Name = "Expenses vs. Budget",
        Source = "ActualVsBudget",
        SourceParameters = "excluded=Savings,Taxes,Income",
        WholeYear = true,
        CustomColumns = "budgetpct,budgetavailable"
    }
};
```

**Why Keep:**
- **Data-driven configuration** means no code changes to add/modify reports
- Clean separation of "what to report" from "how to build reports"
- Easy to understand at a glance
- Can be externalized to JSON/YAML for runtime configuration
- Good for testing (just pass different definitions)
- Non-developers can understand report configurations

**Benefits Realized:**
- Adding new report types is just data entry
- Report definitions are self-documenting
- Easy A/B testing of different report configurations
- Can version control report definitions separately from code

**Verdict:** ✅ **KEEP - Declarative is the right approach**

---

### 5. NamedQuery Pattern for Multi-Series Reports ⭐⭐⭐⭐

**Current Implementation:**
```csharp
class NamedQuery
{
    public string Name { get; set; }  // "Actual", "Budget", "2022", "2023"
    public IQueryable<IReportable> Query { get; set; }
    public bool LeafRowsOnly { get; set; }
    public bool IsMultiSigned { get; set; }
}

// Usage:
var queries = new[]
{
    new NamedQuery { Name = "Actual", Query = actualTransactions },
    new NamedQuery { Name = "Budget", Query = budgetItems }
};

// Result: Report with "Actual" and "Budget" columns
```

**Why Keep:**
- **Clean abstraction** for multi-dimensional reports
- Series name automatically becomes column header
- Enables year-over-year comparisons naturally (series per year)
- LeafRowsOnly flag elegantly handles managed budget filtering
- Makes report building code series-agnostic

**Benefits Realized:**
- Budget vs Actual reports: two named queries
- Year-over-year reports: one query per year
- Can add any number of series without changing report logic
- Custom columns can reference series by name

**Verdict:** ✅ **KEEP - Enables powerful multi-dimensional reports**

---

## Rethink - Needs Improvement (20%)

These areas work but have issues that should be addressed in a rewrite.

### 1. Four-Phase Report Building Pipeline ⭐⭐⭐

**Current Implementation:**
```
Phase 1: Group     - Group reportables by category/month, calculate sums
Phase 2: Place     - Put values into table cells
Phase 3: Propagate - Roll up values to parent categories
Phase 4: Prune     - Remove unnecessary rows
```

**Problems:**
- ❌ **Values change multiple times** (hard to reason about what value means at any point)
- ❌ **Propagation happens in-memory** after query execution (inefficient)
- ❌ **Collector pattern runs during propagation** (confusing timing, hard to debug)
- ❌ **Building things we'll throw away** (prune at end means wasted work)
- ❌ **Hard to unit test** (phases are tightly coupled)

**Example of Confusion:**
```
After Place:   Housing:Mortgage = $2,500
After Propagate: Housing:Mortgage = $2,500 (unchanged)
                Housing = $5,000 (wait, where did this come from?)
After Collection: Housing:Other = $1,200 (collected, but from what?)
```

**Better Approach - 3-Phase Pipeline:**

```
Phase 1: Query & Pre-Aggregate (In Database)
  - Group by category using SQL GROUP BY
  - Let database do the heavy lifting
  - Return only leaf categories with pre-calculated totals
  - Benefits: Faster, scales better, uses database indexes

Phase 2: Build Hierarchy Tree (In Memory, Simple)
  - Parse category strings to create tree structure
  - Calculate parent totals = SUM(children)
  - Clear mental model: parents are always sum of children
  - No mysterious value changes
  - Benefits: Easy to understand, easy to test, predictable

Phase 3: Apply Business Rules (In Memory, After Tree Built)
  - Process collector categories
  - Mark collected items as hidden
  - Apply NumLevels filtering
  - Calculate custom columns
  - Benefits: Each rule independent, order doesn't matter
```

**Concrete Example:**

```csharp
// Phase 1: Database aggregation
var leafCategories = await context.Transactions
    .Where(t => t.Year == year)
    .GroupBy(t => t.Category)
    .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
    .ToListAsync();  // Executes in SQL

// Phase 2: Build tree
var root = new ReportRow { Name = "TOTAL" };
foreach (var leaf in leafCategories)
{
    var path = leaf.Category.Split(':');
    var current = root;
    
    foreach (var segment in path)
    {
        var child = current.Children.FirstOrDefault(c => c.Name == segment)
                    ?? current.AddChild(segment);
        current = child;
    }
    
    current.SeriesValues["Actual"] = leaf.Total;
}

// Walk tree bottom-up to calculate parent totals
root.PropagateFromChildren();  // Simple: parent = sum(children)

// Phase 3: Apply rules
ApplyCollectors(root);  // Separate, clear step
ApplyLevelFiltering(root, numLevels);
CalculateCustomColumns(root);
```

**Benefits of New Approach:**
- ✅ **Clearer data flow:** leaves → parents → rules → display
- ✅ **Better performance:** database does aggregation
- ✅ **Easier to test:** each phase independent
- ✅ **No surprising changes:** values set once, not modified multiple times
- ✅ **Easier to debug:** can inspect tree at any point
- ✅ **Easier to extend:** add new rules without affecting others

**Migration Path:**
1. Implement new 3-phase pipeline in parallel
2. Run both pipelines, compare outputs
3. Once validated, switch to new pipeline
4. Remove old 4-phase code

**Verdict:** 🔄 **RETHINK - Simplify to 3 phases**

---

### 2. Table<TColumn, TRow, TValue> Storage ⭐⭐

**Current Implementation:**
```csharp
class Table<TColumn, TRow, TValue>
{
    private Dictionary<(TColumn, TRow), TValue> _cells;
    
    public TValue this[TColumn col, TRow row]
    {
        get => _cells.GetValueOrDefault((col, row));
        set => _cells[(col, row)] = value;
    }
}

// Usage:
var report = new Report();
report[Report.TotalColumn, FindRow("Housing")] = -5000m;
```

**Problems:**
- ❌ **Generic dictionary is opaque** (hard to inspect in debugger)
- ❌ **No strong typing** on row/column types (strings everywhere)
- ❌ **Loses semantic meaning** (what's a "row" vs "column"?)
- ❌ **Hard to serialize** (custom serialization needed)
- ❌ **Hard to query** (can't use LINQ on tree structure)
- ❌ **No tree semantics** (parent/child relationships hidden)

**Better Approach - Strongly-Typed Tree:**

```csharp
class ReportRow
{
    // Identity
    public string CategoryId { get; set; }        // "Housing:Mortgage"
    public string CategoryName { get; set; }      // "Mortgage"
    
    // Hierarchy
    public int Level { get; set; }                // 0=leaf, 1=parent, etc.
    public ReportRow Parent { get; set; }
    public List<ReportRow> Children { get; set; } = new();
    
    // Data
    public Dictionary<string, decimal> SeriesValues { get; set; } = new();
        // Key: "Actual", "Budget", "2022", "2023"
        // Value: Amount for this series
    
    public Dictionary<string, decimal> CustomColumns { get; set; } = new();
        // Key: "% Progress", "Available"
        // Value: Calculated value
    
    // Metadata
    public bool IsCollected { get; set; }         // Hidden from display?
    public bool IsTotal { get; set; }             // Grand total row?
    public CollectorRule CollectorRule { get; set; } // If this is a collector
    
    // Helper methods
    public void PropagateFromChildren()
    {
        foreach (var series in Children.First().SeriesValues.Keys)
        {
            SeriesValues[series] = Children
                .Where(c => !c.IsCollected)
                .Sum(c => c.SeriesValues.GetValueOrDefault(series));
        }
    }
    
    public ReportRow AddChild(string name)
    {
        var child = new ReportRow 
        { 
            CategoryName = name,
            CategoryId = CategoryId + ":" + name,
            Parent = this,
            Level = Level - 1
        };
        Children.Add(child);
        return child;
    }
}

class Report
{
    // Metadata
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Structure
    public List<string> SeriesNames { get; set; }      // ["Actual", "Budget"]
    public List<ColumnDefinition> CustomColumns { get; set; }
    
    // Data (tree structure)
    public ReportRow Root { get; set; }  // Total row
    
    // Helpers
    public IEnumerable<ReportRow> VisibleRows => 
        Root.Descendants().Where(r => !r.IsCollected);
    
    public IEnumerable<ReportRow> AllRows => Root.Descendants();
}
```

**Benefits of Tree Structure:**
- ✅ **Self-documenting:** Clear what everything is
- ✅ **Easy to serialize:** JSON.stringify(report) just works
- ✅ **Type-safe:** Compiler catches mistakes
- ✅ **Easy to query:** LINQ over tree: `report.AllRows.Where(r => r.Level == 2)`
- ✅ **Easy to test:** Can inspect entire tree structure
- ✅ **Tree semantics explicit:** Parent/Children relationships clear
- ✅ **Easy to debug:** Debugger shows full object structure

**Example Usage:**

```csharp
// Old way (opaque):
var housingTotal = report[Report.TotalColumn, FindRow("Housing")];

// New way (clear):
var housing = report.Root.Children.First(c => c.CategoryName == "Housing");
var total = housing.SeriesValues["Actual"];

// Or with helper:
var housing = report.FindRow("Housing");
var total = housing.SeriesValues["Actual"];
```

**Migration Path:**
1. Create ReportRow parallel to current Table<>
2. Build both in parallel during transition
3. Update consumers one at a time
4. Remove Table<> when all consumers migrated

**Verdict:** 🔄 **RETHINK - Use strongly-typed tree**

---

### 3. Collector Pattern Implementation ⭐⭐

**Current Implementation:**
```csharp
// Line 490: Index collectors during propagation
var depth = row.UniqueID.Count(x => x == ':');
CollectorRows[depth][parentid] = row;

// Line 648: Look up collectors during value placement
var depth = 1 + parentid.Count(x => x == ':');
var collectorRow = CollectorRows.GetValueOrDefault(depth)?
    .GetValueOrDefault(parentid);

// Line 663: Parse collection rule with regex
var regex = new Regex(@"(.+?)\[(.+?)\]");
var match = regex.Match(categoryString);
```

**Problems:**
- ❌ **Regex parsing in hot path** (performance cost on every category)
- ❌ **Depth calculation bug** (off-by-one between index and lookup, documented in Section 7.6.1)
- ❌ **Mixed with propagation logic** (hard to understand when collection happens)
- ❌ **Collection during value placement** (confusing timing)
- ❌ **No explicit "IsCollected" flag** (collected items appear in report)
- ❌ **Hard to test** (tightly coupled with propagation)

**Better Approach:**

```csharp
// Parse collector rules ONCE during report definition load
class CollectorRule
{
    public string CategoryId { get; set; }          // "Housing:Other"
    public string ParentId { get; set; }            // "Housing"
    public bool IsNegation { get; set; }            // true if rule starts with ^
    public HashSet<string> MatchNames { get; set; } // ["Mortgage", "Utilities"]
    
    // Parse once, not repeatedly
    public static CollectorRule Parse(string categoryString)
    {
        // Pattern: "Housing:Other[^Mortgage;Utilities]"
        var match = Regex.Match(categoryString, @"^(.+?)\[(.+?)\]$");
        if (!match.Success)
            return null;
            
        var fullCategory = match.Groups[1].Value;
        var rule = match.Groups[2].Value;
        
        var isNegation = rule.StartsWith('^');
        var names = rule.TrimStart('^')
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        var lastColon = fullCategory.LastIndexOf(':');
        var parentId = lastColon >= 0 
            ? fullCategory.Substring(0, lastColon) 
            : "";
        
        return new CollectorRule
        {
            CategoryId = fullCategory,
            ParentId = parentId,
            IsNegation = isNegation,
            MatchNames = new HashSet<string>(names)
        };
    }
    
    // Check if a sibling category matches this rule
    public bool Matches(string siblingCategoryName)
    {
        var lastSegment = siblingCategoryName.Split(':').Last();
        var inSet = MatchNames.Contains(lastSegment);
        
        // XOR: matches if (in set and not negated) or (not in set and negated)
        return IsNegation ? !inSet : inSet;
    }
}

// Apply collections in separate phase AFTER hierarchy is built
class CollectionProcessor
{
    public void ApplyCollections(Report report)
    {
        // Find all collector rows
        var collectors = report.AllRows
            .Where(r => r.CollectorRule != null)
            .ToList();
        
        foreach (var collector in collectors)
        {
            // Find siblings that match the rule
            var siblings = collector.Parent.Children
                .Where(c => c != collector && 
                       collector.CollectorRule.Matches(c.CategoryId))
                .ToList();
            
            // For each series (Actual, Budget, etc.)
            foreach (var seriesName in report.SeriesNames)
            {
                // Sum matching siblings
                var collectedTotal = siblings
                    .Sum(s => s.SeriesValues.GetValueOrDefault(seriesName));
                
                collector.SeriesValues[seriesName] = collectedTotal;
            }
            
            // Mark collected items as hidden
            foreach (var sibling in siblings)
            {
                sibling.IsCollected = true;
            }
        }
    }
}
```

**Benefits of New Approach:**
- ✅ **Parse rules once** during definition load, not per-value
- ✅ **No depth calculation bugs** (no depth needed!)
- ✅ **Separate concern** not mixed with propagation
- ✅ **Explicit hiding** of collected items (IsCollected flag)
- ✅ **Easy to test** independently
- ✅ **Clear timing** (after tree built, before display)
- ✅ **Better performance** (no repeated regex parsing)

**Example:**

```csharp
// Setup (once):
var rule = CollectorRule.Parse("Housing:Other[^Mortgage;Utilities]");
var collectorRow = new ReportRow 
{ 
    CategoryName = "Other",
    CategoryId = "Housing:Other",
    CollectorRule = rule 
};

// Build tree normally...

// Then apply collections (separate phase):
var processor = new CollectionProcessor();
processor.ApplyCollections(report);

// Result:
// - Housing:Insurance and Housing:Repairs are marked IsCollected=true
// - Housing:Other contains their sum
// - Display layer filters out rows where IsCollected=true
```

**Migration Path:**
1. Implement CollectorRule and CollectionProcessor
2. Add IsCollected flag to current system
3. Run both old and new collection logic, compare results
4. Switch to new processor
5. Remove old collection code

**Verdict:** 🔄 **RETHINK - Separate phase with explicit hiding**

---

### 4. Custom Column Calculations ⭐⭐⭐

**Current Implementation:**
```csharp
// Lambda functions with string-based column access
new ColumnLabel
{
    Name = "% Progress",
    DisplayAsPercent = true,
    Custom = (cols) =>
    {
        var budget = cols.GetValueOrDefault("ID:Budget");
        var actual = cols.GetValueOrDefault("ID:Actual");
        
        if (budget == 0 || Math.Abs(actual / budget) > 10m)
            return 0;
            
        return actual / budget;
    }
}
```

**Problems:**
- ❌ **Lambda functions powerful but hard to validate**
- ❌ **String-based column lookups** (`"ID:Budget"` - what does ID mean?)
- ❌ **No compile-time checking** (typo in string = runtime error)
- ❌ **Hard to debug** (can't step into lambda easily)
- ❌ **Hard to unit test** (need full report context)
- ❌ **No documentation** (what does this calculation do?)

**Better Approach:**

```csharp
// Interface for custom columns
interface ICustomColumn
{
    string Name { get; }
    bool DisplayAsPercent { get; }
    decimal Calculate(ReportRow row, Report report);
}

// Concrete implementations (self-documenting!)
class BudgetProgressColumn : ICustomColumn
{
    public string Name => "% Progress";
    public bool DisplayAsPercent => true;
    
    public decimal Calculate(ReportRow row, Report report)
    {
        var budget = row.SeriesValues.GetValueOrDefault("Budget");
        var actual = row.SeriesValues.GetValueOrDefault("Actual");
        
        // Handle edge cases
        if (budget == 0)
            return 0;
        
        var ratio = actual / budget;
        
        // Cap at 1000% to avoid display issues
        if (Math.Abs(ratio) > 10m)
            return 0;
            
        return ratio;
    }
}

class BudgetAvailableColumn : ICustomColumn
{
    public string Name => "Available";
    public bool DisplayAsPercent => false;
    
    public decimal Calculate(ReportRow row, Report report)
    {
        var actual = row.SeriesValues.GetValueOrDefault("Actual");
        var budget = row.SeriesValues.GetValueOrDefault("Budget");
        
        // Available = Actual - Budget
        // Positive means under budget (good for expenses)
        // Negative means over budget (bad for expenses)
        return actual - budget;
    }
}

class PercentOfTotalColumn : ICustomColumn
{
    public string Name => "% of Total";
    public bool DisplayAsPercent => true;
    
    public decimal Calculate(ReportRow row, Report report)
    {
        var rowTotal = row.SeriesValues.GetValueOrDefault("Actual");
        var grandTotal = report.Root.SeriesValues.GetValueOrDefault("Actual");
        
        return grandTotal == 0 ? 0 : rowTotal / grandTotal;
    }
}

// Registration
class CustomColumnRegistry
{
    private Dictionary<string, ICustomColumn> _columns = new()
    {
        ["budgetpct"] = new BudgetProgressColumn(),
        ["budgetavailable"] = new BudgetAvailableColumn(),
        ["pctoftotal"] = new PercentOfTotalColumn()
    };
    
    public ICustomColumn Get(string key) => _columns[key];
}
```

**Benefits of Interface Approach:**
- ✅ **Type-safe access** to row values
- ✅ **Easy to unit test** (mock ReportRow and Report)
- ✅ **Self-documenting** (class name explains what it does)
- ✅ **Refactoring-friendly** (rename safely)
- ✅ **Can add validation** (throw if required series missing)
- ✅ **Can add error handling** (graceful degradation)
- ✅ **Easy to debug** (set breakpoint in Calculate method)
- ✅ **Discoverable** (intellisense shows all columns)

**Example Usage:**

```csharp
// Old way:
report.AddCustomColumn("budgetpct", lambda);

// New way:
var column = _registry.Get("budgetpct");
foreach (var row in report.VisibleRows)
{
    var value = column.Calculate(row, report);
    row.CustomColumns[column.Name] = value;
}
```

**Unit Testing:**

```csharp
[Test]
public void BudgetProgressColumn_ActualHalfOfBudget_Returns50Percent()
{
    // Arrange
    var row = new ReportRow
    {
        SeriesValues = new()
        {
            ["Budget"] = 1000m,
            ["Actual"] = 500m
        }
    };
    var column = new BudgetProgressColumn();
    
    // Act
    var result = column.Calculate(row, null);
    
    // Assert
    Assert.AreEqual(0.5m, result);
}

[Test]
public void BudgetProgressColumn_BudgetZero_ReturnsZero()
{
    var row = new ReportRow
    {
        SeriesValues = new()
        {
            ["Budget"] = 0m,
            ["Actual"] = 500m
        }
    };
    var column = new BudgetProgressColumn();
    
    var result = column.Calculate(row, null);
    
    Assert.AreEqual(0m, result);
}
```

**Migration Path:**
1. Create ICustomColumn interface and implementations
2. Keep lambda approach as fallback
3. Migrate columns one at a time
4. Remove lambda support when all migrated

**Verdict:** 🔄 **RETHINK - Replace lambdas with interface**

---

### 5. QueryBuilder God Class ⭐⭐

**Current Implementation:**
```csharp
class QueryBuilder
{
    // Many responsibilities in one class:
    public IEnumerable<NamedQuery> QueryActual(...) { }
    public IEnumerable<NamedQuery> QueryBudget(...) { }
    public IEnumerable<NamedQuery> QueryActualVsBudget(...) { }
    public IEnumerable<NamedQuery> QueryManagedBudget(...) { }
    public IEnumerable<NamedQuery> QueryYearOverYear(...) { }
    
    // Plus parameter parsing:
    private void ParseSourceParameters(...) { }
    private IQueryable<IReportable> ApplyFilters(...) { }
}
```

**Problems:**
- ❌ **God class** with too many responsibilities
- ❌ **Mixes query construction with parameter parsing**
- ❌ **Hard to extend** (adding new query type = modify existing class)
- ❌ **Hard to test** (need to test entire class for each query type)
- ❌ **Violates Single Responsibility Principle**

**Better Approach - Strategy Pattern:**

```csharp
// Strategy interface
interface IReportQueryStrategy
{
    IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters);
}

// Concrete strategies (one per query type)
class ActualQueryStrategy : IReportQueryStrategy
{
    public IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters)
    {
        var query = data.Get<Transaction>()
            .Where(t => t.Timestamp.Year == parameters.Year)
            .Where(t => t.Timestamp.Month <= parameters.Month)
            .Where(t => t.Hidden != true)
            .Where(t => !t.Splits.Any());
        
        var splits = data.Get<Split>()
            .Where(s => s.Transaction.Timestamp.Year == parameters.Year)
            .Where(s => s.Transaction.Timestamp.Month <= parameters.Month);
        
        var combined = query.Cast<IReportable>()
            .Concat(splits.Cast<IReportable>());
        
        return new[]
        {
            new NamedQuery 
            { 
                Name = "Actual", 
                Query = combined,
                IsMultiSigned = true
            }
        };
    }
}

class BudgetQueryStrategy : IReportQueryStrategy
{
    public IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters)
    {
        var query = data.Get<BudgetTx>()
            .Where(b => b.Timestamp.Year == parameters.Year);
        
        return new[]
        {
            new NamedQuery 
            { 
                Name = "Budget", 
                Query = query.Cast<IReportable>(),
                IsMultiSigned = true
            }
        };
    }
}

class ActualVsBudgetQueryStrategy : IReportQueryStrategy
{
    private readonly ActualQueryStrategy _actualStrategy;
    private readonly BudgetQueryStrategy _budgetStrategy;
    
    public ActualVsBudgetQueryStrategy(
        ActualQueryStrategy actualStrategy,
        BudgetQueryStrategy budgetStrategy)
    {
        _actualStrategy = actualStrategy;
        _budgetStrategy = budgetStrategy;
    }
    
    public IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters)
    {
        // Compose: reuse existing strategies
        var actual = _actualStrategy.BuildQueries(data, parameters);
        var budget = _budgetStrategy.BuildQueries(data, parameters);
        
        return actual.Concat(budget);
    }
}

class ManagedBudgetQueryStrategy : IReportQueryStrategy
{
    public IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters)
    {
        var managed = data.Get<BudgetTx>()
            .Where(b => b.Timestamp.Year == parameters.Year)
            .Where(b => b.Frequency > 1)
            .AsEnumerable()
            .SelectMany(b => b.Reportables
                .Where(r => r.Timestamp.Month <= parameters.Month))
            .AsQueryable();
        
        return new[]
        {
            new NamedQuery 
            { 
                Name = "Budget", 
                Query = managed,
                LeafRowsOnly = true  // Special flag for managed budgets
            }
        };
    }
}

class YearOverYearQueryStrategy : IReportQueryStrategy
{
    private readonly ActualQueryStrategy _actualStrategy;
    
    public YearOverYearQueryStrategy(ActualQueryStrategy actualStrategy)
    {
        _actualStrategy = actualStrategy;
    }
    
    public IEnumerable<NamedQuery> BuildQueries(
        IDataProvider data, 
        ReportParameters parameters)
    {
        var queries = new List<NamedQuery>();
        
        // Create one query per year
        for (int year = parameters.StartYear; year <= parameters.EndYear; year++)
        {
            var yearParams = parameters with { Year = year };
            var yearQuery = _actualStrategy.BuildQueries(data, yearParams).First();
            
            queries.Add(new NamedQuery
            {
                Name = year.ToString(),
                Query = yearQuery.Query,
                IsMultiSigned = yearQuery.IsMultiSigned
            });
        }
        
        return queries;
    }
}

// Coordinator class (much simpler!)
class QueryBuilder
{
    private readonly IDataProvider _data;
    private readonly Dictionary<string, IReportQueryStrategy> _strategies;
    
    public QueryBuilder(IDataProvider data)
    {
        _data = data;
        
        // Register strategies
        var actual = new ActualQueryStrategy();
        var budget = new BudgetQueryStrategy();
        
        _strategies = new Dictionary<string, IReportQueryStrategy>
        {
            ["Actual"] = actual,
            ["Budget"] = budget,
            ["ActualVsBudget"] = new ActualVsBudgetQueryStrategy(actual, budget),
            ["ManagedBudget"] = new ManagedBudgetQueryStrategy(),
            ["YearOverYear"] = new YearOverYearQueryStrategy(actual)
        };
    }
    
    public IEnumerable<NamedQuery> Build(
        ReportDefinition definition, 
        ReportParameters parameters)
    {
        var strategy = _strategies[definition.Source];
        var queries = strategy.BuildQueries(_data, parameters);
        
        // Apply filters from SourceParameters
        return ApplyFilters(queries, definition.SourceParameters);
    }
    
    private IEnumerable<NamedQuery> ApplyFilters(
        IEnumerable<NamedQuery> queries, 
        string parameters)
    {
        // Parse "excluded=Savings,Taxes,Income"
        // Apply to each query
        // Return filtered queries
    }
}
```

**Benefits of Strategy Pattern:**
- ✅ **Single Responsibility** - Each strategy does one thing
- ✅ **Open/Closed Principle** - Add new strategies without modifying existing
- ✅ **Easy to extend** - New query type = new class
- ✅ **Easy to test** - Test each strategy independently
- ✅ **Composable** - ActualVsBudget reuses Actual and Budget strategies
- ✅ **Clear dependencies** - Constructor injection shows what each needs

**Testing:**

```csharp
[Test]
public void ActualQueryStrategy_FiltersHiddenTransactions()
{
    // Arrange
    var mockData = new MockDataProvider();
    mockData.Transactions.Add(new Transaction { Hidden = true });
    mockData.Transactions.Add(new Transaction { Hidden = false });
    
    var strategy = new ActualQueryStrategy();
    var parameters = new ReportParameters { Year = 2023 };
    
    // Act
    var result = strategy.BuildQueries(mockData, parameters);
    
    // Assert
    var query = result.First().Query.ToList();
    Assert.AreEqual(1, query.Count);
    Assert.IsFalse(query[0].Hidden);
}
```

**Migration Path:**
1. Create strategy interface and implementations
2. Keep existing QueryBuilder methods
3. Have methods delegate to strategies internally
4. Update consumers to use new interface
5. Remove old methods when all consumers migrated

**Verdict:** 🔄 **RETHINK - Split into strategy pattern**

---

## Add - Missing Features

These are capabilities the current system lacks that should be added in a rewrite.

### 1. Report Caching Layer

**Problem:**
Reports regenerate on every page view, even if underlying data hasn't changed. For large datasets or complex reports, this is wasteful.

**Solution:**

```csharp
interface IReportCache
{
    Report Get(string cacheKey);
    void Set(string cacheKey, Report report, TimeSpan expiry);
    void Invalidate(string pattern);
    bool TryGet(string cacheKey, out Report report);
}

class ReportCacheService : IReportCache
{
    private readonly IMemoryCache _cache;
    
    public Report Get(string cacheKey)
    {
        return _cache.Get<Report>(cacheKey);
    }
    
    public void Set(string cacheKey, Report report, TimeSpan expiry)
    {
        _cache.Set(cacheKey, report, expiry);
    }
    
    public void Invalidate(string pattern)
    {
        // Pattern: "report:*:2023:*" invalidates all 2023 reports
        // Implementation depends on cache provider
    }
    
    public bool TryGet(string cacheKey, out Report report)
    {
        return _cache.TryGetValue(cacheKey, out report);
    }
}

// Cache key format: "report:{slug}:{year}:{month}:{level}"
// Example: "report:expenses-v-budget:2023:12:2"

class ReportBuilder
{
    private readonly IReportCache _cache;
    
    public async Task<Report> Build(ReportParameters parameters)
    {
        var cacheKey = $"report:{parameters.Slug}:{parameters.Year}:" +
                       $"{parameters.Month}:{parameters.Level}";
        
        if (_cache.TryGet(cacheKey, out var cachedReport))
        {
            return cachedReport;
        }
        
        var report = await BuildInternal(parameters);
        
        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(30));
        
        return report;
    }
}

// Invalidate cache on data changes
class TransactionRepository
{
    private readonly IReportCache _cache;
    
    public async Task Add(Transaction transaction)
    {
        await _db.Transactions.AddAsync(transaction);
        await _db.SaveChangesAsync();
        
        // Invalidate all reports for this year
        _cache.Invalidate($"report:*:{transaction.Timestamp.Year}:*");
    }
}

class BudgetTxRepository
{
    private readonly IReportCache _cache;
    
    public async Task Update(BudgetTx budget)
    {
        _db.BudgetTxs.Update(budget);
        await _db.SaveChangesAsync();
        
        // Invalidate all budget reports for this year
        _cache.Invalidate($"report:*budget*:{budget.Timestamp.Year}:*");
    }
}
```

**Benefits:**
- ✅ **Much faster page loads** (30s → 50ms for complex reports)
- ✅ **Reduced database load**
- ✅ **Better user experience**
- ✅ **Scales better** with more concurrent users

**Considerations:**
- Cache invalidation is hard (be aggressive to avoid stale data)
- Memory usage (set reasonable expiry times)
- Distributed caching for multi-server deployments

---

### 2. Drill-Down / Detail Views

**Problem:**
When a collector category shows "$3,200" collected from multiple categories, users can't see the detail without manually looking at transactions.

**Solution:**

```csharp
class ReportRow
{
    // ... existing properties ...
    
    // New properties for drill-down
    public string DrillDownUrl { get; set; }
    public List<ReportRow> CollectedItems { get; set; } = new();
    
    public void AddCollectedItem(ReportRow item)
    {
        CollectedItems.Add(item);
        item.IsCollected = true;
    }
}

// UI component
<tr class="report-row" data-level="@row.Level">
    <td class="category">
        @if (row.CollectorRule != null)
        {
            <button type="button" 
                    class="btn-link" 
                    data-toggle="collapse" 
                    data-target="#detail-@row.CategoryId">
                <i class="icon-expand"></i>
                @row.CategoryName
            </button>
        }
        else
        {
            @row.CategoryName
        }
    </td>
    <td class="amount">@row.SeriesValues["Actual"].ToString("C")</td>
</tr>

@if (row.CollectorRule != null && row.CollectedItems.Any())
{
    <tr id="detail-@row.CategoryId" class="collapse detail-row">
        <td colspan="100%">
            <div class="collected-items">
                <h5>Collected Items:</h5>
                <table class="table table-sm">
                    @foreach (var item in row.CollectedItems)
                    {
                        <tr>
                            <td>@item.CategoryName</td>
                            <td>@item.SeriesValues["Actual"].ToString("C")</td>
                            <td>
                                <a href="/Transactions?category=@item.CategoryId&year=@Year">
                                    View Transactions
                                </a>
                            </td>
                        </tr>
                    }
                </table>
            </div>
        </td>
    </tr>
}
```

**Example:**

```
Report Display:
  Housing:Mortgage      $30,000
  Housing:Utilities     $3,800
  Housing:Other ⊕       $3,200    [Click to expand]
  ---
  Housing Total         $37,000

After clicking ⊕:
  Housing:Mortgage      $30,000
  Housing:Utilities     $3,800
  Housing:Other ⊖       $3,200    [Click to collapse]
    ↳ Insurance         $2,400    [View Transactions]
    ↳ Repairs           $600      [View Transactions]
    ↳ HOA               $200      [View Transactions]
  ---
  Housing Total         $37,000
```

**Benefits:**
- ✅ **Better transparency** - Users can see what's collected
- ✅ **Easy navigation** to transaction details
- ✅ **Maintains clean default view** (collapsed by default)
- ✅ **Progressive disclosure** - show detail on demand

---

### 3. Report Validation

**Problem:**
Bad report definitions fail at runtime with cryptic errors. Hard to debug.

**Solution:**

```csharp
interface IReportValidator
{
    ValidationResult Validate(ReportDefinition definition);
}

class ReportDefinitionValidator : IReportValidator
{
    private readonly QueryBuilder _queryBuilder;
    private readonly CustomColumnRegistry _columnRegistry;
    
    public ValidationResult Validate(ReportDefinition definition)
    {
        var errors = new List<string>();
        
        // Check slug
        if (string.IsNullOrWhiteSpace(definition.slug))
            errors.Add("Slug is required");
        
        // Check name
        if (string.IsNullOrWhiteSpace(definition.Name))
            errors.Add("Name is required");
        
        // Check source exists
        if (!_queryBuilder.HasStrategy(definition.Source))
            errors.Add($"Unknown source: {definition.Source}");
        
        // Check custom columns exist
        if (!string.IsNullOrEmpty(definition.CustomColumns))
        {
            var columns = definition.CustomColumns.Split(',');
            foreach (var col in columns)
            {
                if (!_columnRegistry.Has(col.Trim()))
                    errors.Add($"Unknown custom column: {col}");
            }
        }
        
        // Check source parameters are valid
        if (!string.IsNullOrEmpty(definition.SourceParameters))
        {
            var valid = TryParseSourceParameters(
                definition.SourceParameters, 
                out var parseError);
            
            if (!valid)
                errors.Add($"Invalid SourceParameters: {parseError}");
        }
        
        // Check NumLevels is reasonable
        if (definition.NumLevels < 1 || definition.NumLevels > 10)
            errors.Add("NumLevels must be between 1 and 10");
        
        // Check mutual exclusions
        if (definition.WithMonthColumns && !definition.WholeYear)
            errors.Add("WithMonthColumns requires WholeYear=true");
        
        return new ValidationResult(errors);
    }
}

class ValidationResult
{
    public List<string> Errors { get; }
    public bool IsValid => !Errors.Any();
    
    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? new List<string>();
    }
}

// Use at startup
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IReportValidator, ReportDefinitionValidator>();
        
        // Validate all report definitions at startup
        var validator = services.BuildServiceProvider()
            .GetRequiredService<IReportValidator>();
        
        foreach (var definition in ReportBuilder.AllDefinitions)
        {
            var result = validator.Validate(definition);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid report definition '{definition.slug}': " +
                    string.Join(", ", result.Errors));
            }
        }
    }
}
```

**Benefits:**
- ✅ **Fail fast** - Catch errors at startup, not runtime
- ✅ **Better error messages** - Clear what's wrong
- ✅ **Confidence** - Know all reports are valid
- ✅ **Documentation** - Validation rules document requirements

---

### 4. Incremental Report Building

**Problem:**
Building a full-year report with 12 months of data can be slow for large datasets.

**Solution:**

```csharp
class IncrementalReportBuilder
{
    private readonly IReportCache _cache;
    private readonly ReportBuilder _builder;
    
    public async Task<Report> BuildFullYear(ReportParameters parameters)
    {
        // Check if we have full year cached
        var fullYearKey = $"report:{parameters.Slug}:{parameters.Year}:full";
        if (_cache.TryGet(fullYearKey, out var cached))
            return cached;
        
        // Build/retrieve each month
        var monthlyReports = new List<Report>();
        for (int month = 1; month <= 12; month++)
        {
            var monthKey = $"report:{parameters.Slug}:{parameters.Year}:{month}";
            
            if (!_cache.TryGet(monthKey, out var monthReport))
            {
                var monthParams = parameters with { Month = month };
                monthReport = await _builder.Build(monthParams);
                _cache.Set(monthKey, monthReport, TimeSpan.FromHours(24));
            }
            
            monthlyReports.Add(monthReport);
        }
        
        // Combine monthly reports into full year
        var fullYear = CombineMonthlyReports(monthlyReports);
        
        _cache.Set(fullYearKey, fullYear, TimeSpan.FromHours(24));
        
        return fullYear;
    }
    
    private Report CombineMonthlyReports(List<Report> monthlyReports)
    {
        // Create report with month columns
        var combined = new Report
        {
            Name = monthlyReports.First().Name,
            SeriesNames = Enumerable.Range(1, 12)
                .Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m))
                .ToList()
        };
        
        // Build unified tree structure
        // For each category across all months, sum the values
        // ...
        
        return combined;
    }
}

// Invalidation is smarter - only invalidate affected months
class TransactionRepository
{
    public async Task Add(Transaction transaction)
    {
        await _db.Transactions.AddAsync(transaction);
        await _db.SaveChangesAsync();
        
        var year = transaction.Timestamp.Year;
        var month = transaction.Timestamp.Month;
        
        // Only invalidate this month and full year
        _cache.Invalidate($"report:*:{year}:{month}");
        _cache.Invalidate($"report:*:{year}:full");
        // Months not affected remain cached!
    }
}
```

**Benefits:**
- ✅ **Faster initial load** - Build one month instead of 12
- ✅ **Efficient caching** - Cache each month independently
- ✅ **Smart invalidation** - Only invalidate affected months
- ✅ **Better for progressive loading** - Show months as they load

---

## Implementation Roadmap

### Phase 1: Foundation (Weeks 1-2)

**Goals:** Set up new data structures without breaking existing system

**Tasks:**
1. Create `ReportRow` class with tree structure
2. Create `ICustomColumn` interface
3. Create `IReportQueryStrategy` interface
4. Create `CollectorRule` parser
5. Add `IsCollected` flag to existing system
6. Create `IReportCache` interface
7. Write unit tests for all new components

**Deliverable:** New classes coexist with old system, fully tested

### Phase 2: Query Layer (Week 3)

**Goals:** Migrate query building to strategy pattern

**Tasks:**
1. Implement all query strategies
2. Create new `QueryBuilder` coordinator
3. Run both old and new in parallel, compare outputs
4. Fix any discrepancies
5. Switch to new query builder
6. Remove old query methods

**Deliverable:** All queries using strategy pattern

### Phase 3: Report Building (Weeks 4-5)

**Goals:** Migrate to 3-phase pipeline with tree structure

**Tasks:**
1. Implement Phase 1: Query & Pre-Aggregate
2. Implement Phase 2: Build Hierarchy
3. Implement Phase 3: Apply Business Rules
4. Run both old and new pipelines, compare outputs
5. Fix any discrepancies
6. Switch to new pipeline
7. Remove old 4-phase code

**Deliverable:** All reports using new 3-phase pipeline

### Phase 4: Collectors & Custom Columns (Week 6)

**Goals:** Clean up collector and custom column implementations

**Tasks:**
1. Migrate to `CollectionProcessor`
2. Implement hiding of collected items
3. Migrate custom columns to `ICustomColumn`
4. Update UI to filter `IsCollected` rows
5. Add drill-down support for collectors

**Deliverable:** Collectors working correctly with hiding

### Phase 5: Caching & Performance (Week 7)

**Goals:** Add caching and optimize performance

**Tasks:**
1. Implement `ReportCacheService`
2. Add cache invalidation to repositories
3. Implement incremental report building
4. Performance testing and optimization
5. Load testing

**Deliverable:** Cached reports with fast page loads

### Phase 6: Quality & Documentation (Week 8)

**Goals:** Polish and document

**Tasks:**
1. Implement report validation
2. Add comprehensive unit tests (>80% coverage)
3. Add integration tests
4. Update documentation
5. Migration guide for consumers
6. Code review and refinement

**Deliverable:** Production-ready system

---

## Migration Strategy

### Parallel Implementation

**Approach:** Run old and new systems side-by-side, gradually migrate

**Steps:**

1. **Build New Alongside Old**
   - New code in separate namespace (`YoFi.Reports.V3`)
   - No changes to existing code initially
   - Full test coverage of new code

2. **Prove Equivalence**
   - Run both systems on same inputs
   - Compare outputs (should be identical)
   - Log any discrepancies
   - Fix until 100% match

3. **Feature Flag**
   ```csharp
   var useNewReports = _config.GetValue<bool>("Features:NewReports");
   var report = useNewReports 
       ? await _newBuilder.Build(params)
       : await _oldBuilder.Build(params);
   ```

4. **Gradual Rollout**
   - Week 1: 10% of traffic to new system
   - Week 2: 25% of traffic
   - Week 3: 50% of traffic
   - Week 4: 100% of traffic
   - Monitor errors, performance, user feedback

5. **Remove Old Code**
   - After 2 weeks at 100% with no issues
   - Remove old implementation
   - Clean up feature flags

### Rollback Plan

**If problems occur:**
1. Set feature flag to 0% (instant rollback)
2. Investigate issue in new system
3. Fix and redeploy
4. Resume rollout

**Success Criteria:**
- All existing reports produce identical output
- Performance same or better
- No increase in error rate
- User satisfaction maintained

---

## Conclusion

The current YoFi budget reports architecture is **fundamentally sound** with excellent core design patterns. The recommended approach is **refinement, not revolution**:

### Keep (80%):
- ✅ IReportable abstraction
- ✅ Hierarchical string categories
- ✅ BudgetTx.Reportables
- ✅ Declarative ReportDefinition
- ✅ NamedQuery multi-series

### Improve (20%):
- 🔄 Simplify to 3-phase pipeline
- 🔄 Use strongly-typed tree structure
- 🔄 Separate collector phase with hiding
- 🔄 Replace lambdas with interfaces
- 🔄 Split QueryBuilder into strategies

### Add (new):
- ➕ Report caching
- ➕ Drill-down views
- ➕ Report validation
- ➕ Incremental building

This approach preserves what works while addressing known issues and adding valuable capabilities. The result will be a cleaner, faster, more maintainable system that builds on proven design patterns.

---

**End of Document**
