﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Configuration;
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
                    Wiek = x.DeathDate == null ?
                    (DateTime.Now.Year - (x.BirthDate == null ? DateTime.Now.Year : x.BirthDate.Value.Year)).ToString(CultureInfo.InvariantCulture)
                    : (x.DeathDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year).ToString(CultureInfo.InvariantCulture)
                }).ToList());
            }

        }

        public ActionResult Edit(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var person = db.Query<Person>(x => x.Name == id).First();
                var model = new EditViewModel
                {
                    BirthDate =
                        person.BirthDate == null ? "Nie podano" : person.BirthDate.GetValueOrDefault().ToShortDateString(),
                    DeatDate =
                        person.DeathDate == null ? "Nie podano" : person.DeathDate.GetValueOrDefault().ToShortDateString(),
                    Name = person.Name,
                    OldName = person.Name,
                    Sex = person.Sex
                };
                ViewBag.SexList = new List<SelectListItem>
                {
                    new SelectListItem {Selected = person.Sex==Sex.Kobieta, Text = "Kobieta", Value = "Kobieta"},
                    new SelectListItem {Selected = person.Sex==Sex.Mężczyzna, Text = "Mężczyzna", Value = "Mężczyzna"},
                };
                return View(model);
            }
        }

        [HttpPost]
        public ActionResult Edit(EditViewModel model)
        {
            ViewBag.SexList = new List<SelectListItem>
                {
                    new SelectListItem {Selected = model.Sex==Sex.Kobieta, Text = "Kobieta", Value = "Kobieta"},
                    new SelectListItem {Selected = model.Sex==Sex.Mężczyzna, Text = "Mężczyzna", Value = "Mężczyzna"},
                };
            if (ModelState.IsValid)
            {
                using (var db = Db4oEmbedded.OpenFile(DbPath))
                {
                    var isValidName = db.Query<Person>(x => x.Name == model.Name).Count == 0;
                    var personToEdit = db.Query<Person>(x => x.Name == model.OldName).First();
                    if (isValidName || personToEdit.Name == model.Name)
                    {
                        personToEdit.Name = model.Name;
                        //Zmiana płci
                        if (personToEdit.Sex != model.Sex)
                        {
                            if (personToEdit.Childs.Count != 0)
                            {
                                ModelState.AddModelError("Sex", "Przed zmianą płci usuń wszytkie powiązania rodzicielskie!");
                                return View(model);
                            }
                            personToEdit.Sex = model.Sex;
                        }
                        DateTime? deathTime;
                        try
                        {
                            deathTime = Convert.ToDateTime(model.DeatDate);
                        }
                        catch (Exception)
                        {
                            deathTime = null;
                        }
                        var newBirthDate = Convert.ToDateTime(model.BirthDate);
                        if ((newBirthDate - deathTime.GetValueOrDefault()).TotalDays > 0)
                        {
                            ModelState.AddModelError("BirthDate", "Data urodzenia musi być wcześniejsza niż data śmierci.");
                            return View(model);
                        }
                        //Zmiana daty urodzenia
                        if (Convert.ToDateTime(model.BirthDate) != personToEdit.BirthDate.GetValueOrDefault())
                        {
                            foreach (var child in personToEdit.Childs)
                            {
                                if (child.BirthDate != null && personToEdit.BirthDate != null)
                                {
                                    if (!(child.BirthDate.Value.Year - newBirthDate.Year >= 12
                                        && child.BirthDate.Value.Year - newBirthDate.Year <= 70)
                                        && personToEdit.Sex == Sex.Mężczyzna)
                                    {
                                        ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do któregoś z dzieci tej osoby.");
                                        return View(model);
                                    }
                                    if (!(child.BirthDate.Value.Year - newBirthDate.Year >= 10
                                          && child.BirthDate.Value.Year - newBirthDate.Year <= 60))
                                    {
                                        ModelState.AddModelError("BirthDate",
                                            "Nowa data urodzenia nie pasuje do któregoś z dzieci tej osoby.");
                                        return View(model);
                                    }
                                }
                            }
                            if (personToEdit.Father != null)
                            {
                                if (!(newBirthDate.Year - personToEdit.Father.BirthDate.Value.Year >= 12 &&
                                    newBirthDate.Year - personToEdit.Father.BirthDate.Value.Year <= 70))
                                {
                                    ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do któregoś z rodziców tej osoby.");
                                    return View(model);
                                }
                            }
                            if (personToEdit.Mother != null)
                            {
                                if (!(newBirthDate.Year - personToEdit.Mother.BirthDate.Value.Year >= 10 &&
                                    newBirthDate.Year - personToEdit.Mother.BirthDate.Value.Year <= 60))
                                {
                                    ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do któregoś z rodziców tej osoby.");
                                    return View(model);
                                }
                            }
                            personToEdit.BirthDate = newBirthDate;
                        }
                        //zmiana daty śmierci

                        if (deathTime != null && deathTime.GetValueOrDefault() != personToEdit.DeathDate.GetValueOrDefault())
                        {
                            var newDeathDate = Convert.ToDateTime(model.DeatDate);
                            foreach (var child in personToEdit.Childs)
                            {
                                if (child.BirthDate != null && personToEdit.DeathDate != null)
                                {
                                    if (!((child.BirthDate - newDeathDate).Value.TotalDays < 270) && personToEdit.Sex == Sex.Mężczyzna)
                                    {
                                        ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do któregoś z dzieci tej osoby.");
                                        return View(model);
                                    }
                                    if (!((child.BirthDate - newDeathDate).Value.TotalDays <= 0))
                                    {
                                        ModelState.AddModelError("BirthDate",
                                            "Nowa data urodzenia nie pasuje do któregoś z dzieci tej osoby.");
                                        return View(model);
                                    }
                                    personToEdit.DeathDate = newDeathDate;
                                }
                            }
                        }
                        db.Store(personToEdit);
                        return RedirectToAction("Details", new { id = personToEdit.Name });
                    }
                    ModelState.AddModelError("Name", "Istnieje już taka osoba.");
                }
            }
            return View(model);
        }

        public ActionResult AddChild(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem {Selected = true, Text = "Wybierz dziecko", Value = "1"}
                };
                var parent = db.Query<Person>(x => x.Name == id).First();
                if (parent.Sex == Sex.Mężczyzna)
                {
                    list.AddRange(db.Query<Person>(x => x.Father == null
                        && x.BirthDate != null
                        && x.BirthDate.GetValueOrDefault().Year - parent.BirthDate.GetValueOrDefault().Year >= 12
                        && x.BirthDate.GetValueOrDefault().Year - parent.BirthDate.GetValueOrDefault().Year <= 70
                        && (parent.DeathDate == null || (x.BirthDate.GetValueOrDefault() - parent.DeathDate.GetValueOrDefault()).TotalDays <= 270))
                        .Select(k => new SelectListItem
                        {
                            Text = k.Name,
                            Value = k.Name
                        }));
                }
                else
                {
                    list.AddRange(db.Query<Person>(x => x.Mother == null
                        && x.BirthDate != null
                        && x.BirthDate.GetValueOrDefault().Year - parent.BirthDate.GetValueOrDefault().Year >= 10
                        && x.BirthDate.GetValueOrDefault().Year - parent.BirthDate.GetValueOrDefault().Year <= 60
                        && (parent.DeathDate == null || (x.BirthDate.GetValueOrDefault() - parent.DeathDate.GetValueOrDefault()).TotalDays <= 0))
                        .Select(k => new SelectListItem
                        {
                            Text = k.Name,
                            Value = k.Name
                        }));
                }
                ViewBag.childList = list;
                return View(new AddRelationViewModel { Id = parent.Name });
            }

        }

        [HttpPost]
        public ActionResult AddChild(AddRelationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var parent = db.Query<Person>(x => x.Name == model.Id).First();
                var child = db.Query<Person>(x => x.Name == model.ChildId).First();
                parent.Childs.Add(child);
                db.Store(parent.Childs);
                if (parent.Sex == Sex.Mężczyzna)
                    child.Father = parent;
                else
                    child.Mother = parent;
                db.Store(child);
                return RedirectToAction("Details", new { id = model.Id });
            }
        }

        public ActionResult AddFather(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem {Selected = true, Text = "Wybierz ojca", Value = "1"}
                };
                var child = db.Query<Person>(x => x.Name == id).First();

                list.AddRange(db.Query<Person>(x => x.BirthDate != null && x.Sex == Sex.Mężczyzna
                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year >= 12
                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year <= 70
                    && (child.DeathDate == null || (child.BirthDate.GetValueOrDefault() - x.DeathDate.GetValueOrDefault()).TotalDays <= 270))
                    .Select(k => new SelectListItem
                    {
                        Text = k.Name,
                        Value = k.Name
                    }));

                ViewBag.parent = list;
                return View("AddFather", new AddRelationViewModel { ChildId = id });
            }
        }

        public ActionResult AddMother(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem {Selected = true, Text = "Wybierz matkę", Value = "1"}
                };
                var child = db.Query<Person>(x => x.Name == id).First();

                list.AddRange(db.Query<Person>(x => x.BirthDate != null && x.Sex == Sex.Kobieta
                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year >= 10
                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year <= 60
                    && (x.DeathDate == null || (child.BirthDate.GetValueOrDefault() - x.DeathDate.GetValueOrDefault()).TotalDays <= 0))
                    .Select(k => new SelectListItem
                    {
                        Text = k.Name,
                        Value = k.Name
                    }));

                ViewBag.parent = list;
                return View("AddMother", new AddRelationViewModel { ChildId = id });
            }
        }

        [HttpPost]
        public ActionResult AddMother(AddRelationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.Id != "1")
            {
                using (var db = Db4oEmbedded.OpenFile(DbPath))
                {
                    var parent = db.Query<Person>(x => x.Name == model.Id).First();
                    var child = db.Query<Person>(x => x.Name == model.ChildId).First();
                    parent.Childs.Add(child);
                    db.Store(parent.Childs);
                    child.Mother = parent;
                    db.Store(child);
                    return RedirectToAction("Details", new { id = model.ChildId });
                }
            }
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem {Selected = true, Text = "Wybierz matkę", Value = "1"}
                };
                var child = db.Query<Person>(x => x.Name == model.ChildId).First();

                list.AddRange(db.Query<Person>(x => x.BirthDate != null && x.Sex == Sex.Kobieta
                                                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year >= 10
                                                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year <= 60
                                                    && (x.DeathDate == null || (child.BirthDate.GetValueOrDefault() - x.DeathDate.GetValueOrDefault()).TotalDays <= 0))
                    .Select(k => new SelectListItem
                    {
                        Text = k.Name,
                        Value = k.Name
                    }));

                ViewBag.parent = list;
                ModelState.AddModelError("id", "Wybierz dobrą matkę!");
                return View(model);
            }

        }

        [HttpPost]
        public ActionResult AddFather(AddRelationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.Id != "1")
            {
                using (var db = Db4oEmbedded.OpenFile(DbPath))
                {
                    var parent = db.Query<Person>(x => x.Name == model.Id).First();
                    var child = db.Query<Person>(x => x.Name == model.ChildId).First();
                    parent.Childs.Add(child);
                    db.Store(parent.Childs);
                    child.Father = parent;
                    db.Store(child);
                    return RedirectToAction("Details", new { id = model.ChildId });
                }
            }
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem {Selected = true, Text = "Wybierz ojca", Value = "1"}
                };
                var child = db.Query<Person>(x => x.Name == model.ChildId).First();

                list.AddRange(db.Query<Person>(x => x.BirthDate != null && x.Sex == Sex.Mężczyzna
                                                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year >= 12
                                                    && child.BirthDate.GetValueOrDefault().Year - x.BirthDate.GetValueOrDefault().Year <= 70
                                                    && (x.DeathDate == null || (child.BirthDate.GetValueOrDefault() - x.DeathDate.GetValueOrDefault()).TotalDays <= 270))
                    .Select(k => new SelectListItem
                    {
                        Text = k.Name,
                        Value = k.Name
                    }));

                ViewBag.parent = list;
                ModelState.AddModelError("id", "Wybierz dobrego ojca!");
                return View(model);
            }
        }

        public ActionResult Details(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var plz = db.Query<Person>(x => x.Name == id).Select(x => new DetailsViewModel
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
                    Childs = x.Childs.Select(k => new DetailsPersonShared
                    {
                        Name = k.Name,
                        BirthDate = k.BirthDate == null ? "Nie podano" : k.BirthDate.GetValueOrDefault().ToShortDateString(),
                        DeathDate = k.DeathDate == null ? "Nie podano" : k.DeathDate.GetValueOrDefault().ToShortDateString(),
                        Sex = k.Sex.ToString(),
                    }).ToList()
                }).First();
                return View(plz);
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

        public ActionResult DeleteRelation(string id, string childId)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var person = db.Query<Person>(x => x.Name == id).First();
                var childName = person.Childs.Find(x => x.Name == childId).Name;
                return View(new DeleteRelationViewModel
                {
                    ChildId = childName,
                    Id = person.Name
                });
            }
        }

        [HttpPost, ActionName("DeleteRelation")]
        public ActionResult DeleteRelationPost(string id, string childId)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var person = db.Query<Person>(x => x.Name == id).First();
                var childName = person.Childs.Find(x => x.Name == childId);
                person.Childs.Remove(childName);
                if (person.Sex == Sex.Kobieta)
                    childName.Mother = null;
                else
                    childName.Father = null;
                db.Store(person.Childs);
                db.Store(childName);
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeletePost(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var person = db.Query<Person>(x => x.Name == id).First();
                if (person.Father != null)
                {
                    person.Father.Childs.Remove(person.Father.Childs.Find(x => x.Name == id));
                    db.Store(person.Father.Childs);
                }
                if (person.Mother != null)
                {
                    person.Mother.Childs.Remove(person.Mother.Childs.Find(x => x.Name == id));
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
                    if (model.DeatDate != null && model.BirthDate != null)
                        if ((model.DeatDate - model.BirthDate).GetValueOrDefault().TotalDays < 0)
                        {
                            InitializeDropdownLists();
                            ModelState.AddModelError("BirthDate", "Data śmierci nie może być wcześniejsza od daty urodzin!");
                            return View(model);
                        }
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
                        if (model.MotherName != null)
                        {
                            var mother = db.Query<Person>(x => x.Name == model.MotherName).First();
                            person.Mother = mother;
                            mother.Childs.Add(person);
                            db.Store(mother.Childs);
                        }
                        db.Store(person);
                        return RedirectToAction("Index");
                    }
                    ModelState.AddModelError("Name", "Istnieje już taka osoba!");
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
                    (x.BirthDate != null && (x.BirthDate.Value.Year - date.Year) <= -12) &&
                    (x.BirthDate != null && (x.BirthDate.Value.Year - date.Year) >= -70) &&
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
                new SelectListItem {Selected = false, Text = "Kobieta", Value = "Kobieta"},
                new SelectListItem {Selected = false, Text = "Mężczyzna", Value = "Mężczyzna"},
            };
        }
    }
}