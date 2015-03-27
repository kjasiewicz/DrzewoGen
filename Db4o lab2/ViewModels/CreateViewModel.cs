using System;
using System.ComponentModel.DataAnnotations;
using Db4o_lab2.Models;

namespace Db4o_lab2.ViewModels
{
    public class CreateViewModel
    {
        [Required]
        public string Name { get; set; }

        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? DeatDate { get; set; }

        public Sex Sex { get; set; }
    }
}