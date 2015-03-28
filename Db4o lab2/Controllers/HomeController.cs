using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Db4objects.Db4o;
using Db4o_lab2.Models;
using Db4o_lab2.ViewModels;

namespace Db4o_lab2.Controllers
{
    public class HomeController : Controller
    {
        private const string DbPath = "C://plzzzzz";

        // GET: Home
        public ActionResult Index()
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                return View(db.Query<Person>().Select(x => new IndexViewModel
                {
                    FatherName = x.Father == null ? "Nie podano" : x.Father.Name,
                    MotherName = x.Mother == null ? "Nie podano" : x.Mother.Name,
                    Name = x.Name,
                    NumOfChilds = x.Childs.Count,
                    Sex = x.Sex.ToString(),
                    Wiek = x.DeathDate == null ? (DateTime.Now.Year - (x.BirthDate==null?DateTime.Now.Year:x.BirthDate.Value.Year)).ToString(CultureInfo.InvariantCulture) : "Nie żyje :("
                }).ToList());
            }

        }



        public ActionResult Details(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                return View(db.Query<Person>(x=>x.Name==id).Select(x=> new DetailsViewModel
                {
                    FatherName = x.Father != null ? x.Father.Name : "Nie podano",
                    MotherName = x.Mother != null ? x.Mother.Name : "Nie podano",
                    Person = new DetailsPersonShared
                    {
                        Name = x.Name,
                        BirthDate = x.BirthDate == null ? "Nie podano" : x.BirthDate.GetValueOrDefault().ToShortDateString(),
                        DeathDate = x.DeathDate == null ? "Nie podano" : x.DeathDate.GetValueOrDefault().ToShortDateString(),
                        Sex = x.Sex.ToString(),
                    },
                    Childs = x.Childs.Select(k=>new DetailsPersonShared
                    {
                        Name = k.Name,
                        BirthDate = k.BirthDate == null ? "Nie podano" : k.BirthDate.GetValueOrDefault().ToShortDateString(),
                        DeathDate = k.DeathDate == null ? "Nie podano" : k.DeathDate.GetValueOrDefault().ToShortDateString(),
                        Sex = k.Sex.ToString(),
                    }).ToList()
                }).First());
            }
        }

        public ActionResult Delete(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                return View(db.Query<Person>(x => x.Name == id).Select(x => new DeleteViewModel
                {
                    Name = x.Name,
                    Sex = x.Sex.ToString()
                }).First());
            }
        }

        [HttpPost,ActionName("Delete")]
        public ActionResult DeletePost(string name)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var person = db.Query<Person>(x => x.Name == name).First();
                if (person.Father != null)
                {
                    person.Father.Childs.Remove(person.Father.Childs.Find(x => x.Name == name));
                    db.Store(person.Father.Childs);
                }
                if (person.Mother != null)
                {
                    person.Mother.Childs.Remove(person.Mother.Childs.Find(x => x.Name == name));
                    db.Store(person.Mother.Childs);
                }
                foreach (var child in person.Childs)
                {
                    if (person.Sex == Sex.Mężczyzna)
                    {
                        child.Father = null;
                        db.Store(child);
                    }
                    else
                    {
                        child.Mother = null;
                        db.Store(child);
                    }
                }
                db.Delete(person);
                return RedirectToAction("Index");
            }
        }

        public ActionResult Create()
        {
            InitializeDropdownLists();
            return View();
        }

        [HttpPost]
        public ActionResult Create(CreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = Db4oEmbedded.OpenFile(DbPath))
                {
                    if (db.Query<Person>(x => x.Name == model.Name).Count == 0)
                    {
                        var person = new Person
                        {
                            BirthDate = model.BirthDate,
                            DeathDate = model.DeatDate,
                            Childs = new List<Person>(),
                            Name = model.Name,
                            Sex = model.Sex
                        };
                        if (model.FatherName != null)
                        {
                            var father = db.Query<Person>(x => x.Name == model.FatherName).First();
                            person.Father = father;
                            father.Childs.Add(person);
                            db.Store(father.Childs);
                        }
                        if (model.MotherName != null && model.MotherName != "1")
                        {
                            var mother = db.Query<Person>(x => x.Name == model.MotherName).First();
                            person.Mother = mother;
                            mother.Childs.Add(person);
                            db.Store(mother.Childs);
                        }
                        db.Store(person);
                        return RedirectToAction("Index");
                    }
                    ModelState.AddModelError("Name","Istnieje już taka osoba!");
                }
            }
            InitializeDropdownLists();
            return View(model);
        }

        //Return valid Fathers/Mothers names by child birth date
        public JsonResult ReturnFathersMothersNames(DateTime date)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var fatherNames = new List<object>
                {
                    new {Text = "Wybierz ojca", Value = ""}
                };
                fatherNames.AddRange(db.Query<Person>().Where(x =>
                    (x.BirthDate!=null && (x.BirthDate.Value.Year - date.Year) <= -12) &&
                    (x.BirthDate!=null &&(x.BirthDate.Value.Year - date.Year) >= -70) &&
                    (x.DeathDate == null || (date - x.DeathDate).Value.TotalDays < 270) &&
                    x.Sex == Sex.Mężczyzna).Select(k => new
                    {
                        Text = k.Name,
                        Value = k.Name
                    }).ToList());

                var motherNames = new List<object>
                {
                    new {Text = "Wybierz matkę", Value = ""}
                };
                motherNames.AddRange(db.Query<Person>().Where(x =>
                    (x.BirthDate != null && (x.BirthDate.Value.Year - date.Year) <= -10) &&
                    (x.BirthDate != null && (x.BirthDate.Value.Year - date.Year) >= -60) &&
                    (x.DeathDate == null || (date - x.DeathDate).Value.TotalDays <= 0) &&
                    x.Sex == Sex.Kobieta).Select(k => new
                    {
                        Text = k.Name,
                        Value = k.Name
                    }).ToList());
                return Json(new
                {
                    fatherNames,
                    motherNames
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult FillDb()
        {
            var rootChilds = new List<Person>
            {
                new Person
                {
                    BirthDate = new DateTime(2002, 1, 12),
                    Childs = new List<Person>(),
                    Name = "Dziecko 1",
                    Sex = Sex.Kobieta
                },
                new Person
                {
                    BirthDate = new DateTime(2001, 1, 12),
                    Childs = new List<Person>(),
                    Name = "Dziecko 2",
                    Sex = Sex.Mężczyzna
                },
                new Person
                {
                    BirthDate = new DateTime(2000, 1, 12),
                    Childs = new List<Person>(),
                    Name = "Dziecko 3",
                    Sex = Sex.Kobieta
                }
            };
            var rootFemale = new Person
            {
                Name = "Pierwsza matka",
                BirthDate = new DateTime(1950, 5, 15),
                Childs = new List<Person>(),
                Sex = Sex.Kobieta
            };
            var rootMale = new Person
            {
                Name = "Pierwszy ojciec",
                BirthDate = new DateTime(1958, 5, 15),
                Childs = new List<Person>(),
                Sex = Sex.Mężczyzna
            };
            System.IO.File.Delete(DbPath);
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                foreach (var rootChild in rootChilds)
                {
                    rootChild.Father = rootMale;
                    rootChild.Mother = rootFemale;
                    rootFemale.Childs.Add(rootChild);
                    rootMale.Childs.Add(rootChild);
                }
                db.Store(rootFemale);
                db.Store(rootFemale.Childs);
                db.Store(rootMale);
                db.Store(rootMale.Childs);
            }
            return View();
        }

        private void InitializeDropdownLists()
        {
            ViewBag.FatherList = new List<SelectListItem>
            {
                new SelectListItem {Selected = true, Text = "Imię ojca", Value = ""},
            };
            ViewBag.MotherList = new List<SelectListItem>
            {
                new SelectListItem {Selected = true, Text = "Imię matki", Value = ""},
            };
            ViewBag.SexList = new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Kobieta", Value = "1"},
                new SelectListItem {Selected = false, Text = "Mężczyzna", Value = "2"},
            };
        }
    }
}