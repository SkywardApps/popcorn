using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Skyward.Popcorn;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PopcornNetStandardTest
{
    [TestClass]
    public class ExpanderTests
    {
        /// <summary>
        /// A test root object with a myriad of properties used to test direct implementations.
        /// The property names should generally be named for what they are being used to test.
        /// </summary>
        public class RootObject
        {
            public Guid Id { get; set; }
            public Guid? Nullable { get; set; }
            public string StringValue { get; set; }
            public string NonIncluded { get; set; }
            public string ExcludedFromProjection { get; set; }
            public int Upconvert { get; set; }
            public double Downconvert { get; set; }

            public ChildObject Child { get; set; }
            public ChildObject Child2 { get; set; }
            public ChildObject ChildExcludedFromProjection { get; set; }
            public List<ChildObject> Children { get; set; }
            public List<ChildObject> Children2 { get; set; }
            public List<UnprojectedChildObject> ChildListExcludedFromProjection { get; set; }
            public IEnumerable<ChildObject> ChildrenInterface { get; set; }
            public HashSet<ChildObject> ChildrenSet { get; set; }
            public DerivedChildObject SubclassInOriginal { get; set; }
            public ChildObject SuperclassInOriginal { get; set; }
            public Guid InvalidCastType { get; set; }

            public string FromMethod() { return nameof(FromMethod); }
            public ChildObject ComplexFromMethod() { return new ChildObject { Id = Guid.NewGuid(), Name = "ComplexFromMethod ChildObject", Description = "This proves that an object returned from a method will also be projected." }; }
        }

        [InternalOnly]
        public class InternalObjectTrue : RootObject
        {

        }

        [InternalOnly(false)]
        public class InternalObjectFalse : RootObject
        {

        }

        public class InternalObjectValues : RootObject
        {
            [InternalOnly]
            public object InternalPropertyTrue { get; set; }

            [InternalOnly(false)]
            public object InternalPropertyFalse { get; set; }

            [InternalOnly]
            public object InternalMethodTrue() { return new object(); }

            [InternalOnly(false)]
            public object InternalMethodFalse() { return new object(); }
        }

        public class InternalObjectValuesProjection
        {
            public object InternalPropertyTrue { get; set; }
            public object InternalPropertyFalse { get; set; }
            public object InternalMethodTrue { get; set; }
            public object InternalMethodFalse { get; set; }
        }

        /// <summary>
        /// A sub-entity used to test collections
        /// </summary>
        public class ChildObject
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// A sub-entity that is not projected  used to test collections
        /// </summary>
        public class UnprojectedChildObject
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// A subclass of the child entity so we can test polymorphism
        /// </summary>
        public class DerivedChildObject : ChildObject
        {
            public string ExtendedProperty { get; set; }
        }

        /// <summary>
        /// A projection of our testing class.  This mostly contains the same properties, sometimes with different types.
        /// There will be some values here that are calculated just from 'translators'.
        /// </summary>
        public class RootObjectProjection
        {
            //public string Excluded { get; set; } // this one doesn't exist in the projection
            public string Additional { get; set; } // this one doesn't exist in the root


            public Guid Id { get; set; } // a non-simple type
            public Guid? Nullable { get; set; }
            public string StringValue { get; set; }
            public string NonIncluded { get; set; }

            public double? Upconvert { get; set; } // we sneakily changed the type here
            public int? Downconvert { get; set; }

            public double? ValueFromTranslator { get; set; }
            public ChildObjectProjection ComplexFromTranslator { get; set; }

            public ChildObjectProjection Child { get; set; }
            public ChildObjectProjection Child2 { get; set; }
            public List<ChildObjectProjection> Children { get; set; }
            public List<ChildObjectProjection> Children2 { get; set; }
            public List<ChildObjectProjection> ChildListExcludedFromProjection { get; set; }
            public IEnumerable<ChildObjectProjection> ChildrenInterface { get; set; }
            public HashSet<ChildObjectProjection> ChildrenSet { get; set; }
            public ChildObjectProjection SubclassInOriginal { get; set; }
            public DerivedChildObjectProjection SuperclassInOriginal { get; set; }
            public string InvalidCastType { get; set; }

            public string FromMethod { get; set; }
            public ChildObjectProjection ComplexFromMethod { get; set; }
        }

        public class IncludeByDefaultRootObjectProjection
        {
            //public string Excluded { get; set; } // this one doesn't exist in the projection
            public string Additional { get; set; } // this one doesn't exist in the root

            [IncludeByDefault]
            public Guid Id { get; set; } // a non-simple type
            public Guid? Nullable { get; set; }
            [IncludeByDefault]
            public string StringValue { get; set; }
            public string NonIncluded { get; set; }

            public double? Upconvert { get; set; } // we sneakily changed the type here
            public int? Downconvert { get; set; }

            public double? ValueFromTranslator { get; set; }
            public ChildObjectProjection ComplexFromTranslator { get; set; }

            public IncludeByDefaultChildObjectProjection Child { get; set; }
            public List<ChildObjectProjection> Children { get; set; }
            public IEnumerable<ChildObjectProjection> ChildrenInterface { get; set; }
            public HashSet<ChildObjectProjection> ChildrenSet { get; set; }
            public IncludeByDefaultChildObjectProjection SubclassInOriginal { get; set; }
            public IncludeByDefaultDerivedChildObjectProjection SuperclassInOriginal { get; set; }
            public string InvalidCastType { get; set; }

            public string FromMethod { get; set; }
            public ChildObjectProjection ComplexFromMethod { get; set; }
        }

        public class ChildObjectProjection
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

        }

        public class IncludeByDefaultChildObjectProjection
        {
            [IncludeByDefault]
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class DerivedChildObjectProjection : ChildObjectProjection
        {
            public string ExtendedProperty { get; set; }
        }

        public class IncludeByDefaultDerivedChildObjectProjection : IncludeByDefaultChildObjectProjection
        {
            [IncludeByDefault]
            public string ExtendedProperty { get; set; }
        }


        public class Loop
        {
            public Loop Next { get; set; }
            public string Name { get; set; }
            public Loop NextWithDefaultIncludes { get; set; }
        }

        public class LoopProjection
        {
            public LoopProjection Next { get; set; }

            public string Name { get; set; }
            [SubPropertyIncludeByDefault("[Name]")]
            public LoopProjection NextWithDefaultIncludes { get; set; }
        }


        public class EntityFromFactory
        {
            public string Name { get; set; }
        }

        public class EntityFromFactoryProjection
        {
            public string Name { get; set; }
            public string ShouldBeEmpty { get; set; }
        }

        public class EntityFromContextBasedFactory
        {
            public string MappedString { get; set; }
            public string ContextString { get; set; }
        }

        public class EntityFromContextBasedFactoryProjection
        {
            public string MappedString { get; set; }
            public string ContextString { get; set; }
        }

        public class NonMappedType
        {
            public string Name { get; set; }
            public string Title { get; set; }
            public List<NonMappedType> Children { get; set; }
        }

        Expander _expander;

        [TestInitialize]
        public void SetupExpanderRegistry()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, RootObjectProjection>($"[{nameof(RootObjectProjection.Id)},{nameof(RootObjectProjection.StringValue)},{nameof(RootObjectProjection.NonIncluded)}]",
                (definition) =>
                {
                    definition.Translate(o => o.ValueFromTranslator, () => 5.2);
                    definition.Translate(o => o.ComplexFromTranslator, () => new ChildObjectProjection { Id = new Guid(), Name = "Complex trans name", Description = "Complex trans description" });
                });

            config.Map<ChildObject, ChildObjectProjection>();
            config.Map<DerivedChildObject, DerivedChildObjectProjection>();
            config.Map<Loop, LoopProjection>();
            config.Map<EntityFromFactory, EntityFromFactoryProjection>();
            config.Map<EntityFromContextBasedFactory, EntityFromContextBasedFactoryProjection>();
            config.Map<InternalObjectValues, InternalObjectValuesProjection>();
            config.AssignFactory<EntityFromFactoryProjection>(() => new EntityFromFactoryProjection { ShouldBeEmpty = "Generated" });
            config.AssignFactory<EntityFromContextBasedFactoryProjection>((context) => new EntityFromContextBasedFactoryProjection{ ContextString = context["DefaultString"] as string, MappedString = context["DefaultString"] as string });
        }

        [TestMethod]
        public void InternalOnlyClassFalse()
        {
            InternalObjectFalse root = new InternalObjectFalse();

            var result = _expander.Expand(root, null);
            result.ShouldBeNull();
        }

        [TestMethod]
        public void InternalOnlyClassTrue()
        {
            InternalObjectTrue root = new InternalObjectTrue();

            Should.Throw<InternalOnlyViolationException>( () => _expander.Expand(root, null));
        }

        [TestMethod]
        public void InternalOnlyValues()
        {
            InternalObjectValues root = new InternalObjectValues();
            Should.Throw<InternalOnlyViolationException>(() => _expander.Expand(root, null, "[InternalMethodTrue]"));
            Should.Throw<InternalOnlyViolationException>(() => _expander.Expand(root, null, "[InternalPropertyTrue]"));
            var result = (InternalObjectValuesProjection)_expander.Expand(root, null, "[InternalMethodFalse]");
            result.InternalMethodFalse.ShouldBeNull();
            result = (InternalObjectValuesProjection)_expander.Expand(root, null, "[InternalPropertyFalse]");
            result.InternalPropertyFalse.ShouldBeNull();
        }

        // Things to test
        // Specific includes
        [TestMethod]
        public void SimpleMapping()
        {
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Id)},{nameof(RootObjectProjection.StringValue)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.StringValue.ShouldBe(root.StringValue);
            projection.Id.ShouldBe(root.Id);

            projection.NonIncluded.ShouldBeNull();
            projection.Additional.ShouldBeNull();
            projection.Child.ShouldBeNull();
            projection.Children.ShouldBeNull();
            projection.Upconvert.ShouldBeNull();
            projection.ValueFromTranslator.ShouldBeNull();
            projection.Downconvert.ShouldBeNull();
        }

        // assign a null
        [TestMethod]
        public void AssignNull()
        {
            var root = new RootObject
            {
                StringValue = null,
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.StringValue)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.StringValue.ShouldBe(null);
        }

        // spaces in includes
        [TestMethod]
        public void SpacesInIncludes()
        {
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[ {nameof(RootObjectProjection.Id)} , {nameof(RootObjectProjection.StringValue)} ]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.StringValue.ShouldBe(root.StringValue);
            projection.Id.ShouldBe(root.Id);
        }

        // Assign to nullable
        [TestMethod]
        public void AssignValueToNullable()
        {
            var root = new RootObject
            {
                Nullable = Guid.NewGuid(),
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Nullable)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Nullable.ShouldBe(root.Nullable);
        }

        // Assign to nullable
        [TestMethod]
        public void AssignNullToNullable()
        {
            var root = new RootObject
            {
                Nullable = null,
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Nullable)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Nullable.ShouldBe(null);
        }

        // change basic types (upconvert)
        [TestMethod]
        public void UpconvertBasicType()
        {
            var root = new RootObject
            {
                Upconvert = 5
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Upconvert)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Upconvert.ShouldBe(5);
        }

        // change basic types (downconvert)
        [TestMethod]
        public void DownconvertBasicType()
        {
            var root = new RootObject
            {
                Downconvert = 5.5
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Downconvert)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Downconvert.ShouldBe(6);
        }

        // Invalid conversion
        [TestMethod]
        public void InvalidConversion()
        {
            var root = new RootObject
            {
                InvalidCastType = Guid.NewGuid()
            };

            object result = null;
            Shouldly.Should.Throw<InvalidCastException>(() => { result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.InvalidCastType)}]")); }).Message.ShouldBe(nameof(RootObjectProjection.InvalidCastType));
            result.ShouldBeNull();
        }


        // assign to subclass Subclass
        [TestMethod]
        public void ConvertToSubclass()
        {
            var root = new RootObject
            {
                SubclassInOriginal = new DerivedChildObject
                {
                    Name = "Name",
                    ExtendedProperty = "Extended"
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.SubclassInOriginal)}[{nameof(DerivedChildObject.Name)},{nameof(DerivedChildObject.ExtendedProperty)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.SubclassInOriginal.ShouldNotBeNull();

            DerivedChildObjectProjection child = projection.SubclassInOriginal as DerivedChildObjectProjection;
            child.ShouldNotBeNull();
            child.Name.ShouldBe(root.SubclassInOriginal.Name);
            child.ExtendedProperty.ShouldBe(root.SubclassInOriginal.ExtendedProperty);
        }


        // assign to superclass
        [TestMethod]
        public void AssignToSuperclass()
        {
            var root = new RootObject
            {
                SuperclassInOriginal = new ChildObject
                {
                    Name = "Name"
                }
            };

            object result = null;
            Shouldly.Should.Throw<InvalidCastException>(() => { result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.SuperclassInOriginal)}]")); });
            result.ShouldBeNull();
        }

        // assign simple from a method
        [TestMethod]
        public void SimpleFromMethod()
        {
            var root = new RootObject
            {
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.FromMethod)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.FromMethod.ShouldBe(root.FromMethod());
        }

        // assign complex from a method
        [TestMethod]
        public void ComplexFromMethod()
        {
            var root = new RootObject
            {
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ComplexFromMethod)}[{nameof(ChildObjectProjection.Id)},{nameof(ChildObjectProjection.Name)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.ComplexFromMethod.ShouldNotBeNull();
            projection.ComplexFromMethod.Id.ShouldNotBe(Guid.Empty);
            String.IsNullOrWhiteSpace(projection.ComplexFromMethod.Name).ShouldBeFalse();
            projection.ComplexFromMethod.Name.ShouldBe(root.ComplexFromMethod().Name);
            projection.ComplexFromMethod.Description.ShouldBeNull();


        }

        // assign simple from a translator
        [TestMethod]
        public void SimpleFromTranslator()
        {
            var root = new RootObject
            {
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ValueFromTranslator)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.ValueFromTranslator.ShouldBe(5.2);
        }

        // child
        [TestMethod]
        public void ChildIncluded()
        {
            var root = new RootObject
            {
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Name",
                    Description = "Description"
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Child)}[{nameof(ChildObject.Id)},{nameof(ChildObject.Name)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Child.ShouldNotBeNull();
            projection.Child.Id.ShouldBe(root.Child.Id);
            projection.Child.Name.ShouldBe(root.Child.Name);
            projection.Child.Description.ShouldBeNull();
        }


        // list of children 
        [TestMethod]
        public void ListOfChildren()
        {
            var root = new RootObject
            {
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new ChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Children)}[{nameof(ChildObject.Name)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.Children.ShouldNotBeNull();
            projection.Children.Count.ShouldBe(2);
            projection.Children.Any(c => c.Name == "Item1").ShouldBeTrue();
            projection.Children.Any(c => c.Name == "Item2").ShouldBeTrue();
            projection.Children.Any(c => c.Description != null).ShouldBeFalse();
        }

        [TestMethod]
        public void ListOfChildrenInterface()
        {
            var root = new RootObject
            {
                ChildrenInterface = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new ChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ChildrenInterface)}[{nameof(ChildObject.Name)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.ChildrenInterface.ShouldNotBeNull();
            projection.ChildrenInterface.Count().ShouldBe(2);
            projection.ChildrenInterface.Any(c => c.Name == "Item1").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => c.Name == "Item2").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => c.Description != null).ShouldBeFalse();
        }

        /// <summary>
        /// Make sure that we handle polymorphic lists (list<BaseClass> containing DerivedClass)
        /// </summary>
        [TestMethod]
        public void ListOfChildrenInterfaceDerivedClass()
        {
            var root = new RootObject
            {
                ChildrenInterface = new List<ChildObject>
                {
                    new DerivedChildObject
                    {
                        Name = "Item1",
                        Description = "Description1",
                        ExtendedProperty = "ExtendedProperty1"
                    },
                    new DerivedChildObject
                    {
                        Name = "Item2",
                        Description = "Description2",
                        ExtendedProperty = "ExtendedProperty2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ChildrenInterface)}[{nameof(ChildObject.Name)},{nameof(DerivedChildObject.ExtendedProperty)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.ChildrenInterface.ShouldNotBeNull();
            projection.ChildrenInterface.Count().ShouldBe(2);
            projection.ChildrenInterface.All(c => c as DerivedChildObjectProjection != null).ShouldBeTrue();
            projection.ChildrenInterface.Any(c => c.Name == "Item1").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => c.Name == "Item2").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => (c as DerivedChildObjectProjection).ExtendedProperty == "ExtendedProperty1").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => (c as DerivedChildObjectProjection).ExtendedProperty == "ExtendedProperty2").ShouldBeTrue();
            projection.ChildrenInterface.Any(c => c.Description != null).ShouldBeFalse();
        }

        // @TODO Currently this fails because a HashSet doesn't implement IList (rightly!) but our current collection implementation assumes an IList interface is available.
        [TestMethod, Ignore]
        public void ListOfChildrenHashSet()
        {
            var root = new RootObject
            {
                ChildrenSet = new HashSet<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new ChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ChildrenSet)}[{nameof(ChildObject.Name)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.ChildrenSet.ShouldNotBeNull();
            projection.ChildrenSet.Count().ShouldBe(2);
            projection.ChildrenSet.Any(c => c.Name == "Item1").ShouldBeTrue();
            projection.ChildrenSet.Any(c => c.Name == "Item2").ShouldBeTrue();
            projection.ChildrenSet.Any(c => c.Description != null).ShouldBeFalse();
        }

        // assign complex from a translator
        [TestMethod]
        public void ComplexFromTranslator()
        {
            var root = new RootObject
            {
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.ComplexFromTranslator)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.ComplexFromTranslator.Id.ShouldBe(new Guid());
            projection.ComplexFromTranslator.Name.ShouldBe("Complex trans name");
            projection.ComplexFromTranslator.Description.ShouldBe("Complex trans description");
        } 

        // Default
        [TestMethod]
        public void UseDefaultIncludes()
        {
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
            };

            object result = _expander.Expand(root);
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.StringValue.ShouldBe(root.StringValue);
            projection.Id.ShouldBe(root.Id);
            projection.NonIncluded.ShouldBe(root.NonIncluded);

            projection.Additional.ShouldBeNull();
            projection.Child.ShouldBeNull();
            projection.Children.ShouldBeNull();
            projection.Upconvert.ShouldBeNull();
            projection.ValueFromTranslator.ShouldBeNull();
            projection.Downconvert.ShouldBeNull();
        }

        // Default on child
        [TestMethod]
        public void UseDefaultIncludesOnChild()
        {
            var root = new RootObject
            {
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new ChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Children)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.Children.ShouldNotBeNull();
            projection.Children.Count.ShouldBe(2);
            projection.Children.Any(c => c.Name == "Item1").ShouldBeTrue();
            projection.Children.Any(c => c.Name == "Item2").ShouldBeTrue();
            projection.Children.Any(c => c.Description == "Description1").ShouldBeTrue();
            projection.Children.Any(c => c.Description == "Description2").ShouldBeTrue();
        }

        // empty includes
        [TestMethod]
        public void EmptyIncludes()
        {
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new ChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse("[]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.Children.ShouldBeNull();
            projection.StringValue.ShouldBe(root.StringValue);
            projection.Id.ShouldBe(root.Id);

            projection.NonIncluded.ShouldBeNull();
            projection.Additional.ShouldBeNull();
            projection.Child.ShouldBeNull();
            projection.Children.ShouldBeNull();
            projection.Upconvert.ShouldBeNull();
            projection.ValueFromTranslator.ShouldBeNull();
            projection.Downconvert.ShouldBeNull();

        }

        // multiple different children with different includes
        [TestMethod]
        public void DifferingChildren()
        {
            var root = new RootObject
            {
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Child1",
                    Description = "Child Description1"
                },
                Child2 = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Child2",
                    Description = "Child Description2"
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Child)}[{nameof(ChildObjectProjection.Name)}],{nameof(RootObjectProjection.Child2)}[{nameof(ChildObjectProjection.Id)},{nameof(ChildObjectProjection.Description)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.Child.Name.ShouldBe(root.Child.Name);
            projection.Child.Description.ShouldBeNull();
            projection.Child.Id.ShouldBe(Guid.Empty);

            projection.Child2.Name.ShouldBeNull();
            projection.Child2.Description.ShouldBe(root.Child2.Description);
            projection.Child2.Id.ShouldBe(root.Child2.Id);
        }

        // multiple different lists with different includes
        [TestMethod]
        public void ListOfDifferingChildren()
        {
            var root = new RootObject
            {
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "C1Item1",
                        Description = "C1Description1"
                    },
                    new ChildObject
                    {
                        Name = "C1Item2",
                        Description = "C1Description2"
                    }
                },
                Children2 = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "C2Item1",
                        Description = "C2Description1"
                    },
                    new ChildObject
                    {
                        Name = "C2Item2",
                        Description = "C2Description2"
                    }
                }
            };

            object result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObjectProjection.Children)}[{nameof(ChildObjectProjection.Name)}],{nameof(RootObjectProjection.Children2)}[{nameof(ChildObjectProjection.Description)}]]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();

            projection.Children.ShouldNotBeNull();
            projection.Children.Count.ShouldBe(2);
            projection.Children.Any(c => c.Name == "C1Item1").ShouldBeTrue();
            projection.Children.Any(c => c.Name == "C1Item2").ShouldBeTrue();
            projection.Children.Any(c => c.Description != null).ShouldBeFalse();
            projection.Children.Any(c => c.Id != Guid.Empty).ShouldBeFalse();

            projection.Children2.ShouldNotBeNull();
            projection.Children2.Count.ShouldBe(2);
            projection.Children2.Any(c => c.Description == "C2Description1").ShouldBeTrue();
            projection.Children2.Any(c => c.Description == "C2Description2").ShouldBeTrue();
            projection.Children2.Any(c => c.Name != null).ShouldBeFalse();
            projection.Children2.Any(c => c.Id != Guid.Empty).ShouldBeFalse();
        }

        // non-projected child
        [TestMethod]
        public void UnprojectedChild()
        {
            var root = new RootObject
            {
                ChildExcludedFromProjection = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Child1",
                    Description = "Child Description1"
                }
            };

            object result = null;
            Shouldly.Should.Throw<ArgumentOutOfRangeException>(() => { result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObject.ChildExcludedFromProjection)}]")); });
        }

        // non-projected list of children
        [TestMethod]
        public void UnprojectedChildList()
        {
            var root = new RootObject
            {
                ChildListExcludedFromProjection = new List<UnprojectedChildObject>
                {
                    new UnprojectedChildObject
                    {
                        Name = "Item1",
                        Description = "Description1"
                    },
                    new UnprojectedChildObject
                    {
                        Name = "Item2",
                        Description = "Description2"
                    }
                }
            };

            object result = null;
            Shouldly.Should.Throw<ArgumentOutOfRangeException>(() => { result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(RootObject.ChildExcludedFromProjection)}]")); });
        }

        // Database navigation property
        [TestMethod, Ignore]
        public void DatabaseObject()
        {

        }


        [TestMethod]
        public void SelfReferencingLoop()
        {
            var firstObject = new Loop();
            var secondObject = new Loop();
            firstObject.Next = secondObject;
            var thirdObject = new Loop();
            secondObject.Next = thirdObject;
            thirdObject.Next = firstObject;

            Should.Throw<SelfReferencingLoopException>(() => _expander.Expand(firstObject, null, PropertyReference.Parse($"[]")));
        }

        [TestMethod]
        public void SubPropertyDefaultIncludeAttribute()
        {
            var firstObject = new Loop { Name = "firstObject" };
            var secondObject = new Loop { Name = "secondObject" };
            var thirdObject = new Loop { Name = "thirdObject" };

            firstObject.NextWithDefaultIncludes = secondObject;
            secondObject.NextWithDefaultIncludes = thirdObject;
            thirdObject.NextWithDefaultIncludes = firstObject;

            var result = _expander.Expand(firstObject, null, PropertyReference.Parse($"[Name,NextWithDefaultIncludes]"));
            result.ShouldNotBeNull();

            var loopProjection = result as LoopProjection;
            loopProjection.ShouldNotBeNull();
            loopProjection.Name.ShouldBe(firstObject.Name);
            loopProjection.NextWithDefaultIncludes.ShouldNotBeNull();
            loopProjection.NextWithDefaultIncludes.Name.ShouldBe(secondObject.Name);
            loopProjection.NextWithDefaultIncludes.NextWithDefaultIncludes.ShouldBeNull();
        }

        [TestMethod]
        public void DefaultIncludesAttribute()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, IncludeByDefaultRootObjectProjection>();

            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Name",
                    Description = "Description"
                }
            };

            var result = _expander.Expand(root);
            result.ShouldNotBeNull();

            IncludeByDefaultRootObjectProjection projection = result as IncludeByDefaultRootObjectProjection;
            projection.ShouldNotBeNull();
            projection.StringValue.ShouldBe(root.StringValue);
            projection.Id.ShouldBe(root.Id);

            projection.NonIncluded.ShouldBeNull();
            projection.Child.ShouldBeNull();
        }

        // Prove that [IncludeByDefault] grabs properties on a base class for a sub-class
        [TestMethod]
        public void DefaultIncludesAttributePolymorphism()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, IncludeByDefaultRootObjectProjection>();
            config.Map<ChildObject, IncludeByDefaultChildObjectProjection>();
            config.Map<DerivedChildObject, IncludeByDefaultDerivedChildObjectProjection>();

            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                Child = new DerivedChildObject
                {
                    ExtendedProperty = "new extended property",
                    Name = "subclass",
                    Id = Guid.NewGuid()
                }
            };

            var result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(IncludeByDefaultRootObjectProjection.Child)}]"));
            result.ShouldNotBeNull();

            IncludeByDefaultRootObjectProjection projection = result as IncludeByDefaultRootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Id.ShouldBe(Guid.Empty);

            IncludeByDefaultDerivedChildObjectProjection childProjection = projection.Child as IncludeByDefaultDerivedChildObjectProjection;
            childProjection.Name.ShouldBe(null);
            childProjection.Id.ShouldBe(root.Child.Id);
            childProjection.ExtendedProperty.ShouldBe("new extended property");
        }

        // Prove that [IncludeByDefault] grabs properties on a base class for a sub-class, should the base class not actually be mapped
        [TestMethod]
        public void DefaultIncludesAttributePolymorphismNoBaseMapping()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, IncludeByDefaultRootObjectProjection>();
            config.Map<DerivedChildObject, IncludeByDefaultDerivedChildObjectProjection>();

            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                Child = new DerivedChildObject
                {
                    ExtendedProperty = "new extended property",
                    Name = "subclass",
                    Id = Guid.NewGuid()
        }
            };

            var result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(IncludeByDefaultRootObjectProjection.Child)}]"));
            result.ShouldNotBeNull();

            IncludeByDefaultRootObjectProjection projection = result as IncludeByDefaultRootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Id.ShouldBe(Guid.Empty);

            IncludeByDefaultDerivedChildObjectProjection childProjection = projection.Child as IncludeByDefaultDerivedChildObjectProjection;
            childProjection.Name.ShouldBe(null);
            childProjection.Id.ShouldBe(root.Child.Id);
            childProjection.ExtendedProperty.ShouldBe("new extended property");
        }

        // Prove that a DefaultIncludes for a derived class can be declared at map time
        [TestMethod]
        public void DefaultIncludesStringSubClass()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<RootObject, RootObjectProjection>();
            config.Map<DerivedChildObject, DerivedChildObjectProjection>($"[{nameof(DerivedChildObjectProjection.Id)},{nameof(DerivedChildObjectProjection.ExtendedProperty)}]");

            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                Child = new DerivedChildObject
                {
                    ExtendedProperty = "new extended property",
                    Name = "subclass",
                    Id = Guid.NewGuid()
                }
            };

            var result = _expander.Expand(root, null, PropertyReference.Parse($"[{nameof(IncludeByDefaultRootObjectProjection.Child)}]"));
            result.ShouldNotBeNull();

            RootObjectProjection projection = result as RootObjectProjection;
            projection.ShouldNotBeNull();
            projection.Id.ShouldBe(Guid.Empty);

            DerivedChildObjectProjection childProjection = projection.Child as DerivedChildObjectProjection;
            childProjection.Name.ShouldBe(null);
            childProjection.Id.ShouldBe(root.Child.Id);
            childProjection.ExtendedProperty.ShouldBe("new extended property");
        }

        [TestMethod]
        public void MapAndDefaultIncludesAttribute()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            Assert.ThrowsException<MultipleDefaultsException>(() => config.Map<RootObject, IncludeByDefaultRootObjectProjection>($"[{nameof(IncludeByDefaultRootObjectProjection.Id)},{nameof(IncludeByDefaultRootObjectProjection.StringValue)}]"));
        }

        [TestMethod]
        public void CreateWithTypeFactory()
        {
            var entity = new EntityFromFactory();

            var result = _expander.Expand(entity, null, PropertyReference.Parse($"[Name]"));
            result.ShouldNotBeNull();

            var entityProjection = result as EntityFromFactoryProjection;
            entityProjection.ShouldBeEmpty.ShouldBe("Generated");
        }

        [TestMethod]
        public void CreateWithContextBasedTypeFactory()
        {
            var entity = new EntityFromContextBasedFactory() { MappedString = "Some Text" };
            var context = new Dictionary<string, object> { { "DefaultString", "SpecifiedByContext" } };
            var result = _expander.Expand(entity, context, PropertyReference.Parse($"[MappedString]"));
            result.ShouldNotBeNull();

            var entityProjection = result as EntityFromContextBasedFactoryProjection;
            entityProjection.MappedString.ShouldNotBe("SpecifiedByContext");
            entityProjection.ContextString.ShouldBe("SpecifiedByContext");
        }

        [TestMethod]
        public void BlindExpansion()
        {
            new PopcornConfiguration(_expander).EnableBlindExpansion(true);
            var entity = new NonMappedType
            {
                Name = nameof(BlindExpansion),
                Title = "Test",
                Children = new List<NonMappedType>
                {
                    new NonMappedType{
                        Name = "First",
                        Title = "Test",
                    },
                    new NonMappedType{
                        Name = "Second",
                        Title = "Test"
                    },
                    new NonMappedType{
                        Name = "Third",
                        Title = "Test"
                    },
                }
            };

            var result = _expander.Expand(entity, null, PropertyReference.Parse($"[Name,Children[Title]]"));
            result.ShouldNotBeNull();

            var mappedEntity = result as Dictionary<string, object>;
            mappedEntity.ShouldNotBeNull();

            mappedEntity["Name"].ShouldBe(nameof(BlindExpansion));
            mappedEntity.ContainsKey("Title").ShouldBeFalse();
            mappedEntity["Children"].ShouldNotBeNull();
            var children = new List<Dictionary<string, object>>();
            foreach(var item in mappedEntity["Children"] as ArrayList)
            {
                children.Add(item as Dictionary<string, object>);
            };
            children.Count.ShouldBe(3);
            children.Count(c => c.ContainsKey("Name")).ShouldBe(0);
            children.Count(c => c.ContainsKey("Title") && (string)c["Title"] == "Test").ShouldBe(3);
            new PopcornConfiguration(_expander).EnableBlindExpansion(false);
        }

        // Successfully sort a list ascending
        [TestMethod]
        public void SortAscending()
        {
            List<RootObject> holder = new List<RootObject>();
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "1st person",
                NonIncluded = "A description"
            };
            var rootOne = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "2nd person",
                NonIncluded = "A description"
            };
            var rootTwo = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "3rd person",
                NonIncluded = "A description"
            };

            holder.Add(rootOne);
            holder.Add(rootTwo);
            holder.Add(root);

            object objectConversion = holder;
            objectConversion = _expander.Sort(objectConversion, "StringValue", Skyward.Popcorn.SortDirection.Ascending);

            var results = (IList)objectConversion;
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[0]), root.StringValue);
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[1]), rootOne.StringValue);
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[2]), rootTwo.StringValue);
        }

        // Successfully sort a list descending
        [TestMethod]
        public void SortDescending()
        {
            List<RootObject> holder = new List<RootObject>();
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "1st person",
                NonIncluded = "A description"
            };
            var rootOne = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "2nd person",
                NonIncluded = "A description"
            };
            var rootTwo = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "3rd person",
                NonIncluded = "A description"
            };

            holder.Add(rootOne);
            holder.Add(rootTwo);
            holder.Add(root);

            object objectConversion = holder;
            objectConversion = _expander.Sort(objectConversion, "StringValue", Skyward.Popcorn.SortDirection.Descending);

            var results = (IList)objectConversion;
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[0]), rootTwo.StringValue);
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[1]), rootOne.StringValue);
            Assert.AreEqual(root.GetType().GetProperty("StringValue").GetValue(results[2]), root.StringValue);
        }

        // Sort with an Unknown sort direction
        [TestMethod]
        public void SortUnknown()
        {
            List<RootObject> holder = new List<RootObject>();
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "1st person",
                NonIncluded = "A description"
            };
            var rootOne = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "2nd person",
                NonIncluded = "A description"
            };
            var rootTwo = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "3rd person",
                NonIncluded = "A description"
            };

            holder.Add(rootOne);
            holder.Add(rootTwo);
            holder.Add(root);

            object objectConversion = holder;
            Assert.ThrowsException<ArgumentException>(() => _expander.Sort(objectConversion, "StringValue", Skyward.Popcorn.SortDirection.Unknown));
        }

        // Attempt to sort a property that doesn't exist on the object
        [TestMethod]
        public void SortNonExistentProperty()
        {
            List<RootObject> holder = new List<RootObject>();
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "1st person",
                NonIncluded = "A description"
            };
            var rootOne = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "2nd person",
                NonIncluded = "A description"
            };
            var rootTwo = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "3rd person",
                NonIncluded = "A description"
            };

            holder.Add(rootOne);
            holder.Add(rootTwo);
            holder.Add(root);

            object objectConversion = holder;
            Assert.ThrowsException<InvalidCastException>(() => _expander.Sort(objectConversion, "IDontExist", Skyward.Popcorn.SortDirection.Ascending));
        }

        // Attempt to sort a complex object
        [TestMethod]
        public void SortObject()
        {
            List<RootObject> holder = new List<RootObject>();
            var root = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Name",
                    Description = "Description"
                }
            };
            var rootOne = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Name",
                    Description = "Description"
                }
            };
            var rootTwo = new RootObject
            {
                Id = Guid.NewGuid(),
                StringValue = "Name",
                NonIncluded = "A description",
                ExcludedFromProjection = "Some Details",
                Child = new ChildObject
                {
                    Id = Guid.NewGuid(),
                    Name = "Name",
                    Description = "Description"
                }
            };

            holder.Add(rootOne);
            holder.Add(rootTwo);
            holder.Add(root);

            object objectConversion = holder;
            Assert.ThrowsException<ArgumentException>(() => _expander.Sort(objectConversion, "Child", 0));
        }

        // Sort a non IEnumerable object
        [TestMethod]
        public void SortNonIEnumerable()
        {
            int test = 1;

            object objectConversion = test;
            Assert.ThrowsException<ArgumentException>(() => _expander.Sort(objectConversion, "StringValue", Skyward.Popcorn.SortDirection.Ascending));
        }


        [TestMethod]
        public void Authorization()
        {
            new PopcornConfiguration(_expander).Authorize<ChildObject>((source, context, value) => {
                return value.Name == "YES";
            });

            var root = new RootObject
            {
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "YES"
                    },
                    new ChildObject
                    {
                        Name = "YES"
                    },
                    new ChildObject
                    {
                        Name = "NO"
                    },
                    new ChildObject
                    {
                        Name = "NO"
                    },
                    new ChildObject
                    {
                        Name = "YES"
                    }
                },
                Child = new ChildObject
                {
                    Name = "NO"
                },
                Child2 = new ChildObject
                {
                    Name = "YES"
                },
            };

            var result = _expander.Expand(root, includes: PropertyReference.Parse($"[{nameof(RootObjectProjection.Children)},{nameof(RootObjectProjection.Child)},{nameof(RootObjectProjection.Child2)}]")) as RootObjectProjection;
            result.ShouldNotBeNull();
            result.Children.Count.ShouldBe(3);
            result.Children.Any(c => c.Name == "NO").ShouldBeFalse();
            result.Children.All(c => c.Name == "YES").ShouldBeTrue();
            result.Child.ShouldBeNull();
            result.Child2.ShouldNotBeNull();
        }


        [TestMethod]
        public void AuthorizeWithContext()
        {
            new PopcornConfiguration(_expander).Authorize<ChildObject>((source, context, value) => {
                return value.Name ==  context["Name"];
            });

            var root = new RootObject
            {
                Children = new List<ChildObject>
                {
                    new ChildObject
                    {
                        Name = "YES"
                    },
                    new ChildObject
                    {
                        Name = "YES"
                    },
                    new ChildObject
                    {
                        Name = "NO"
                    },
                    new ChildObject
                    {
                        Name = "NO"
                    },
                    new ChildObject
                    {
                        Name = "YES"
                    }
                },
                Child = new ChildObject
                {
                    Name = "NO"
                },
                Child2 = new ChildObject
                {
                    Name = "YES"
                },
            };

            var result = _expander.Expand(
                root, 
                context: new Dictionary<string, object> { ["Name"] = "YES"},
                includes: PropertyReference.Parse($"[{nameof(RootObjectProjection.Children)},{nameof(RootObjectProjection.Child)},{nameof(RootObjectProjection.Child2)}]")) as RootObjectProjection;
            result.ShouldNotBeNull();
            result.Children.Count.ShouldBe(3);
            result.Children.Any(c => c.Name == "NO").ShouldBeFalse();
            result.Children.All(c => c.Name == "YES").ShouldBeTrue();
            result.Child.ShouldBeNull();
            result.Child2.ShouldNotBeNull();

        }
    }
}
