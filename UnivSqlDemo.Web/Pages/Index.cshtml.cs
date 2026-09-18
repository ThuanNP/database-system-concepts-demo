using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UnivSqlDemo.Web.Pages;

/// <summary>
/// Trang duy nhất của ứng dụng. Toàn bộ dữ liệu lấy về qua các điểm cuối
/// /api/... khai báo trong Program.cs, nên trang này chỉ dựng khung giao diện.
/// </summary>
public sealed class IndexModel : PageModel
{
    public void OnGet() { }
}
