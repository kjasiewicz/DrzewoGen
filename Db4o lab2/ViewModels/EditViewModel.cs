using System.ComponentModel.DataAnnotations;
using Db4o_lab2.Models;

namespace Db4o_lab2.ViewModels
{
    public class EditViewModel
    {
        [Required(ErrorMessage = "Pole {0} jest wymagane"), Display(Name = "Imię")]
        public string Name { get; set; }
        [Display(Name = "Data urodzenia")]
        public string BirthDate { get; set; }
        [Display(Name = "Data śmierci")]
        public string DeatDate { get; set; }
        [Display(Name = "Płeć")]
        public Sex Sex { get; set; }
        public string OldName { get; set; }
    }
}