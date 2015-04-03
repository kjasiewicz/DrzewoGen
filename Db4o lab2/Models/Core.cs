using System;
using System.Collections.Generic;

namespace Db4o_lab2.Models
{
    public enum Sex
    {
        Mężczyzna,
        Kobieta
    }

    public class Person
    {
        public string Name { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? DeathDate { get; set; }
        public Sex Sex { get; set; }
        public Person Father { get; set; }
        public Person Mother { get; set; }
        public List<Person> Childs { get; set; }
    }

    public class LcaFilterClass
    {
        public string key { get; set; }
        public string parent { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string birthYear { get; set; }
        public string deathYear { get; set; }
        public bool Ancestor { get; set; }
    }
}