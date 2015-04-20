using System.Collections.Generic;

namespace Db4o_lab2.ViewModels
{
    public class DetailsViewModel
    {
        public DetailsPersonShared Person { get; set; }
        public List<DetailsPersonShared> Childs { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public List<DetailsPersonShared> Inheritors { get; set; } 
    }

    public class DetailsPersonShared
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public string BirthDate { get; set; }
        public string DeathDate { get; set; }
    }
}