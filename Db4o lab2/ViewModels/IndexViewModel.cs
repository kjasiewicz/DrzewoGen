using System;
using System.ComponentModel.DataAnnotations;

namespace Db4o_lab2.ViewModels
{
    public class IndexViewModel
    {
        [Display(Name = "Imię")]
        public string Name { get; set; }
        [Display(Name="Ojciec")]
        public string FatherName { get; set; }
        [Display(Name="Matka")]
        public string MotherName { get; set; }
        [Display(Name="Płeć")]
        public string Sex { get; set; }
        [Display(Name = "Data urodzenia")]
        public string BirthDate { get; set; }
        [Display(Name = "Data śmierci")]
        public string DeathDate { get; set; }
    }
}