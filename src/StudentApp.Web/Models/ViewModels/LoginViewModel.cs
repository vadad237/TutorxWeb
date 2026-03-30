using System.ComponentModel.DataAnnotations;

namespace StudentApp.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}
