using System.ComponentModel.DataAnnotations;

namespace Db4o_lab2.ViewModels
{
    public class DeleteViewModel
    {
        [Display(Name = "Imię")]
        public string Name { get; set; }
        [Display(Name = "Płeć")]
        public string Sex { get; set; }
    }
}