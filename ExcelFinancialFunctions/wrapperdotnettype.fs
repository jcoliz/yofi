// I cannot use F# optional parameters because they end up forcing you to include the F# dll
// Instead I use overloading with parsimony to achieve similar goals
#light
namespace Excel.FinancialFunctions

open Excel.FinancialFunctions.Tvm
open Excel.FinancialFunctions.Loan

/// A wrapper class to expose the Excel financial functions API to .NET clients
type Financial =
    /// The accrued interest for a security that pays periodic interest ([learn more](http://office.microsoft.com/en-us/excel/HP052089791033.aspx))

    /// The cumulative interest paid between two periods ([learn more](http://office.microsoft.com/en-us/excel/HP052090381033.aspx))
    static member CumIPmt (rate, nper, pv, startPeriod, endPeriod, typ) =
        calcCumipmt rate nper pv startPeriod endPeriod typ

    /// The cumulative principal paid on a loan between two periods ([learn more](http://office.microsoft.com/en-us/excel/HP052090391033.aspx))
    static member CumPrinc (rate, nper, pv, startPeriod, endPeriod, typ) =
        calcCumprinc rate nper pv startPeriod endPeriod typ


    /// The future value of an investment ([learn more](http://office.microsoft.com/en-us/excel/HP052090991033.aspx))
    static member Fv (rate, nper, pmt, pv, typ) =
        calcFv rate nper pmt pv typ

    /// The future value of an initial principal after applying a series of compound interest rates ([learn more](http://office.microsoft.com/en-us/excel/HP052091001033.aspx))
    static member FvSchedule (principal, schedule) =
        calcFvSchedule principal schedule
    
    /// The interest payment for an investment for a given period ([learn more](http://office.microsoft.com/en-us/excel/HP052091451033.aspx))
    static member IPmt (rate, per, nper, pv, fv, typ) =
        calcIpmt rate per nper pv fv typ
       
    /// Calculates the interest paid during a specific period of an investment ([learn more](http://office.microsoft.com/en-us/excel/HP052508401033.aspx))
    static member ISPmt (rate, per, nper, pv) =
        calcIspmt rate per nper pv
    
    /// The number of periods for an investment ([learn more](http://office.microsoft.com/en-us/excel/HP052091981033.aspx))
    static member NPer (rate, pmt, pv, fv, typ) =
        calcNper rate pmt pv fv typ


    /// The periodic payment for an annuity ([learn more](http://office.microsoft.com/en-us/excel/HP052092151033.aspx))
    static member Pmt (rate, nper, pv, fv, typ) =
        calcPmt rate nper pv fv typ

    /// The payment on the principal for an investment for a given period ([learn more](http://office.microsoft.com/en-us/excel/HP052092181033.aspx))
    static member PPmt (rate, per, nper, pv, fv, typ) =
        calcPpmt rate per nper pv fv typ
   
    /// The present value of an investment ([learn more](http://office.microsoft.com/en-us/excel/HP052092251033.aspx))
    static member Pv (rate, nper, pmt, fv, typ) =
        calcPv rate nper pmt fv typ

    /// The interest rate per period of an annuity ([learn more](http://office.microsoft.com/en-us/excel/HP052092321033.aspx))
    static member Rate (nper, pmt, pv, fv, typ, guess) =
        calcRate nper pmt pv fv typ guess
    /// The interest rate per period of an annuity ([learn more](http://office.microsoft.com/en-us/excel/HP052092321033.aspx))
    static member Rate (nper, pmt, pv, fv, typ) =
        calcRate nper pmt pv fv typ 0.1