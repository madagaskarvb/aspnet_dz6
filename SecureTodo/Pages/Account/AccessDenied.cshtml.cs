using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecureTodo.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
            Response.StatusCode = 403;
        }
    }
}