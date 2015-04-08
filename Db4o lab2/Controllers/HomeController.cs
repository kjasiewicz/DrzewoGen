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

        // GET: Main view - list of people
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

        // GET: Edit person
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

        // POST: Edit person
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
                                        ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do dziecka "+child.Name.ToUpper()+" tej osoby.");
                                        return View(model);
                                    }
                                    if (!(child.BirthDate.Value.Year - newBirthDate.Year >= 10
                                          && child.BirthDate.Value.Year - newBirthDate.Year <= 60))
                                    {
                                        ModelState.AddModelError("BirthDate",
                                            "Nowa data urodzenia nie pasuje do dziecka "+child.Name.ToUpper()+" tej osoby.");
                                        return View(model);
                                    }
                                }
                            }
                            if (personToEdit.Father != null)
                            {
                                if (!(newBirthDate.Year - personToEdit.Father.BirthDate.Value.Year >= 12 &&
                                    newBirthDate.Year - personToEdit.Father.BirthDate.Value.Year <= 70))
                                {
                                    ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do ojca.");
                                    return View(model);
                                }
                            }
                            if (personToEdit.Mother != null)
                            {
                                if (!(newBirthDate.Year - personToEdit.Mother.BirthDate.Value.Year >= 10 &&
                                    newBirthDate.Year - personToEdit.Mother.BirthDate.Value.Year <= 60))
                                {
                                    ModelState.AddModelError("BirthDate", "Nowa data urodzenia nie pasuje do matki.");
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
                                if (child.BirthDate != null)
                                {
                                    if (!((child.BirthDate - newDeathDate).Value.TotalDays < 270) && personToEdit.Sex == Sex.Mężczyzna)
                                    {
                                        ModelState.AddModelError("BirthDate", "Nowa data śmierci nie pasuje do dziecka "+child.Name.ToUpper()+" tej osoby.");
                                        return View(model);
                                    }
                                    if (!((child.BirthDate - newDeathDate).Value.TotalDays <= 0))
                                    {
                                        ModelState.AddModelError("BirthDate",
                                            "Nowa data śmierci nie pasuje do dziecka "+child.Name.ToUpper()+" tej osoby.");
                                        return View(model);
                                    }
                                    personToEdit.DeathDate = newDeathDate;
                                }
                                
                            }
                            if (personToEdit.DeathDate == null && personToEdit.Childs.Count==0)
                            {
                                personToEdit.DeathDate = newDeathDate;
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

        //GET: Add child to person (preparing dropdown list)
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

        //POST: Add child to person
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

        //GET: Add father
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

        //GET: Add mother
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

        //POST: Add mother
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

        //POST: Add father
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

        //GET: Person details
        public ActionResult Details(string id)
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var inheritors = new List<string>();
                var x = db.Query<Person>(k => k.Name == id).First();
                GetInheritors(x,ref inheritors);
                var plz = new DetailsViewModel
                {
                    FatherName = x.Father != null ? x.Father.Name : "Nie podano",
                    MotherName = x.Mother != null ? x.Mother.Name : "Nie podano",
                    Person = new DetailsPersonShared
                    {
                        Name = x.Name,
                        BirthDate =
                            x.BirthDate == null ? "Nie podano" : x.BirthDate.GetValueOrDefault().ToShortDateString(),
                        DeathDate =
                            x.DeathDate == null ? "Nie podano" : x.DeathDate.GetValueOrDefault().ToShortDateString(),
                        Sex = x.Sex.ToString(),
                    },
                    Childs = x.Childs.Select(k => new DetailsPersonShared
                    {
                        Name = k.Name,
                        BirthDate =
                            k.BirthDate == null ? "Nie podano" : k.BirthDate.GetValueOrDefault().ToShortDateString(),
                        DeathDate =
                            k.DeathDate == null ? "Nie podano" : k.DeathDate.GetValueOrDefault().ToShortDateString(),
                        Sex = k.Sex.ToString(),
                    }).ToList(),
                    Inheritors = inheritors
                };
                return View(plz);
            }
        }

        //GET: Delete person
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

        //GET: Delete relation between 2 people
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

        //POST: Delete relation between 2 people
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

        //POST: Delete person 
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

        //GET: Create new person
        public ActionResult Create()
        {
            InitializeDropdownLists();
            return View();
        }

        //POST: Create new person
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

        //GET: Family tree view
        public ActionResult FamilyTree(string id)
        {
            return View((object)id);
        }

        //AJAX GET: Get family tree data (usage of GetChildrens recursive function)
        public JsonResult GetFamilyTreeData(string id)
        {
            var lista = new HashSet<object>();
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var root = db.Query<Person>(x => x.Name == id).First();
                lista.Add(new
                {
                    key = root.Name,
                    name = root.Name,
                    gender = root.Sex == Sex.Mężczyzna ? "M" : "F",
                    birthYear = root.BirthDate.Value.ToShortDateString(),
                    deathYear = root.DeathDate == null ? "Nie podano" : root.DeathDate.Value.ToShortDateString()
                });
                GetChildrens(root, ref lista);
                return Json(lista, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: Common ancestors view
        public ActionResult CommonAncestors()
        {
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new List<SelectListItem>
                {
                    new SelectListItem{Text="Wybierz korzeń",Value="1"}
                };
                list.AddRange(db.Query<Person>(x => x.Father == null && x.Mother == null && x.BirthDate != null).Select(k => new SelectListItem
                {
                    Text = k.Name,
                    Value = k.Name
                }).ToList());
                ViewBag.RootList = list;
                return View();
            }
        }

        //AJAX GET: List of available names by given root
        public JsonResult GetCommonAncestorsList(string root)
        {
            if (root == "1") return Json(false, JsonRequestBehavior.AllowGet);
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var list = new HashSet<SelectListItem>();
                GetNames(db.Query<Person>(x => x.Name == root).First(), ref list);
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }

        //AJAX GET: Common ancestors for given 2 persons and root - (usage of Lca func)
        public JsonResult GetCommonAncestors(string root, string id1, string id2)
        {
            if (root == "1") return Json(true, JsonRequestBehavior.AllowGet);
            using (var db = Db4oEmbedded.OpenFile(DbPath))
            {
                var roott = db.Query<Person>(x => x.Name == root).First();
                var lista = new HashSet<LcaFilterClass>();
                Lca(roott, ref lista);
                var plz = id1;
                var plz2 = id2;
                var plzLista = new List<LcaFilterClass>
                {
                    db.Query<Person>(x => x.Name == root).Select(k => new LcaFilterClass
                    {
                        name = k.Name,
                        Ancestor = true,
                        birthYear = k.BirthDate.GetValueOrDefault().ToShortDateString(),
                        key = k.Name,
                        gender = k.Sex == Sex.Mężczyzna ? "M" : "F",
                        deathYear = k.DeathDate == null ? "Nie podano" : k.DeathDate.Value.ToShortDateString(),
                        parent = ""
                    }).First()
                };
                do
                {
                    var temp = lista.First(x => x.name == plz);
                    plzLista.Add(temp);
                    plz = temp.parent;
                } while (plz != root);
                do
                {
                    var temp = lista.FirstOrDefault(x => x.name == plz2);
                    if ((plzLista.Find(x => x.name == temp.name)) != null)
                    {
                        if (temp.name != id1 && temp.name != id2)
                            plzLista.Find(x => x.name == temp.name).Ancestor = true;
                    }
                    else
                    {
                        plzLista.Add(temp);
                    }
                    plz2 = temp.parent;
                } while (plz2 != root);
                return Json(plzLista, JsonRequestBehavior.AllowGet);
            }
        }

        //AJAX GET: Return valid Fathers/Mothers names by child birth date
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

        //Fill database with initial data
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

        #region Helpers

        //Get descendants of given root (for family tree view)
        private static void GetChildrens(Person parent, ref HashSet<object> lista)
        {
            foreach (var children in parent.Childs)
            {
                lista.Add(new
                {
                    key = children.Name,
                    parent = parent.Name,
                    name = children.Name,
                    gender = children.Sex == Sex.Mężczyzna ? "M" : "F",
                    birthYear = children.BirthDate.Value.ToShortDateString(),
                    deathYear = children.DeathDate == null ? "Nie podano" : children.DeathDate.Value.ToShortDateString()
                });
                GetChildrens(children, ref lista);
            }
        }

        //Get name of every descendant of given root (parent)
        private static void GetNames(Person parent, ref HashSet<SelectListItem> lista)
        {
            foreach (var children in parent.Childs)
            {
                lista.Add(new SelectListItem
                {
                    Text = children.Name,
                    Value = children.Name
                });
                GetNames(children, ref lista);
            }
        }

        //Get name of every descendant of given root (parent) - LCA algorithm
        public static void Lca(Person parent, ref HashSet<LcaFilterClass> lista)
        {
            foreach (var children in parent.Childs)
            {
                lista.Add(new LcaFilterClass
                {
                    key = children.Name,
                    name = children.Name,
                    parent = parent.Name,
                    gender = children.Sex == Sex.Mężczyzna ? "M" : "F",
                    birthYear = children.BirthDate.Value.ToShortDateString(),
                    deathYear = children.DeathDate == null ? "Nie podano" : children.DeathDate.Value.ToShortDateString()
                });
                Lca(children, ref lista);
            }
        }

        //Initialize dropdown lissts
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

        //Get inheritors of given person and store them into list
        private static void GetInheritors(Person root, ref List<string> inheritors)
        {
            foreach (var inheritor in root.Childs)
            {
                if (inheritor.DeathDate != null)
                    GetInheritors(inheritor, ref inheritors);
                else
                    inheritors.Add(inheritor.Name);
            }
        }

        #endregion
    }
}