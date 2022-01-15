using Microsoft.AspNetCore.Http;

namespace YoFi.AspNet.Pages.Helpers
{
    public class SessionVariables
    {
        private HttpContext _context;

        public SessionVariables(HttpContext context)
        {
            _context = context;
        }

        public int? Year
        {
            get
            {
                int? result = null;
                var value = _context?.Session.GetString(nameof(Year));
                if (!string.IsNullOrEmpty(value))
                    if (int.TryParse(value, out int year))
                        result = year;

                return result;
            }
            set
            {
                var serialisedDate = value.HasValue ? value.ToString() : null;
                _context?.Session.SetString(nameof(Year), serialisedDate);
            }
        }
    }
}
