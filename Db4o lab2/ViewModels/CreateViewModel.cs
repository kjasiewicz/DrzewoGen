using System;
using System.ComponentModel.DataAnnotations;
using Db4o_lab2.Models;

namespace Db4o_lab2.ViewModels
{
    public class CreateViewModel
    {
        [Required(ErrorMessage = "Pole {0} jest wymagane"),Display(Name = "Imię")]
        public string Name { get; set; }
        [Display(Name = "Imię ojca")]
        public string FatherName { get; set; }
        [Display(Name = "Imię matki")]
        public string MotherName { get; set; }
        [Display(Name="Data urodzenia")]
        public DateTime? BirthDate { get; set; }
        [Display(Name = "Data śmierci")]
        public DateTime? DeatDate { get; set; }
        [Display(Name = "Płeć")]
        public Sex Sex { get; set; }
    }
}