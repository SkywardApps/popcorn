using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Skyward.Popcorn;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PopcornNetStandardTest
{
    [TestClass]
    public class MultipleProjectionTests
    {
        #region Test Helper Classes
        const string TestName = "Name";
        const string TestEmail = "Email@Domain.Tld";

        #region Original Data Types
        class User
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name { get; set; } = TestName;
            public string Email { get; set; } = TestEmail;
        }

        class UserRelationship
        {
            public User Owner { get; set; }
            public User Child { get; set; }
        }

        class UserCollection
        {
            public List<User> Users1 { get; set; }
            public List<User> Users2 { get; set; }
        }
        #endregion

        #region Projections
        class UserFullProjection
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        class UserWithNameProjection
        {
            public string Name { get; set; }
        }

        class UserWithEmailProjection
        {
            public string Email { get; set; }
        }


        class UserRelationshipNameProjection
        {
            public UserWithNameProjection Owner { get; set; }
            public UserWithNameProjection Child { get; set; }
        }
        class UserRelationshipEmailProjection
        {
            public UserWithEmailProjection Owner { get; set; }
            public UserWithEmailProjection Child { get; set; }
        }

        class UserRelationshipMixedProjection
        {
            public UserWithNameProjection Owner { get; set; }
            public UserWithEmailProjection Child { get; set; }
        }

        class UserCollectionProjection
        {
            public List<UserWithNameProjection> Users1 { get; set; }
            public List<UserWithEmailProjection> Users2 { get; set; }
        }

        #endregion
        #endregion

        Expander _expander;

        [TestInitialize]
        public void SetupExpanderRegistry()
        {
            _expander = new Expander();
            var config = new PopcornConfiguration(_expander);
            config.Map<UserRelationship, UserRelationship>(config:builder => 
            {
                builder.AlternativeMap<UserRelationshipNameProjection>();
                builder.AlternativeMap<UserRelationshipEmailProjection>();
                builder.AlternativeMap<UserRelationshipMixedProjection>();
            });
            config.Map<User, UserFullProjection>(config: builder =>
            {
                builder.AlternativeMap<UserWithNameProjection>();
                builder.AlternativeMap<UserWithEmailProjection>();
            });
            config.Map<UserCollection, UserCollectionProjection>();
        }

        /// <summary>
        /// Sanity test that default behviour still works with alternative maps
        /// </summary>
        [TestMethod]
        public void MapDefault()
        {
            var relationship = new UserRelationship
            {
                Owner = new User(),
                Child = new User()
            };

            var relationshipProjected = _expander.Expand(relationship, null, includes:"[Owner]") as UserRelationship;
            relationshipProjected.ShouldNotBeNull();
            relationshipProjected.Owner.ShouldNotBeNull();
            relationshipProjected.Owner.Name.ShouldBe(TestName);
            relationshipProjected.Owner.Email.ShouldBe(TestEmail);
            relationshipProjected.Child.ShouldBeNull();
        }

        /// <summary>
        /// Make sure we can map to alternative 1 as needed
        /// </summary>
        [TestMethod]
        public void MapNames()
        {
            var relationship = new UserRelationship
            {
                Owner = new User(),
                Child = new User()
            };

            var relationshipProjected = _expander.Expand<UserRelationshipNameProjection>(relationship);
            relationshipProjected.ShouldNotBeNull();
            relationshipProjected.Owner.ShouldNotBeNull();
            relationshipProjected.Owner.Name.ShouldBe(TestName);
            relationshipProjected.Child.ShouldNotBeNull();
            relationshipProjected.Child.Name.ShouldBe(TestName);
        }

        /// <summary>
        /// Make sure we can map to alternative 2 as needed
        /// </summary>
        [TestMethod]
        public void MapEmails()
        {
            var relationship = new UserRelationship
            {
                Owner = new User(),
                Child = new User()
            };

            var relationshipProjected = _expander.Expand<UserRelationshipEmailProjection>(relationship);
            relationshipProjected.ShouldNotBeNull();
            relationshipProjected.Owner.ShouldNotBeNull();
            relationshipProjected.Owner.Email.ShouldBe(TestEmail);
            relationshipProjected.Child.ShouldNotBeNull();
            relationshipProjected.Child.Email.ShouldBe(TestEmail);
        }

        /// <summary>
        /// Make sure we can map to a combination of alternatives 1 and 2 at the same time
        /// </summary>
        [TestMethod]
        public void MapMixed()
        {
            var relationship = new UserRelationship
            {
                Owner = new User(),
                Child = new User()
            };

            var relationshipProjected = _expander.Expand<UserRelationshipMixedProjection>(relationship);
            relationshipProjected.ShouldNotBeNull();
            relationshipProjected.Owner.ShouldNotBeNull();
            relationshipProjected.Owner.Name.ShouldBe(TestName);
            relationshipProjected.Child.ShouldNotBeNull();
            relationshipProjected.Child.Email.ShouldBe(TestEmail);
        }


        /// <summary>
        /// And make sure that alternatives apply acceptably to collections as well.
        /// </summary>
        [TestMethod]
        public void MapCollection()
        {
            var collection = new UserCollection
            {
                Users1 = new List<User> {
                    new User(),
                    new User(),
                },
                Users2 = new List<User>
                {
                    new User(),
                    new User(),
                }
            };

            var collectionProjected = _expander.Expand<UserCollectionProjection>(collection);
            collectionProjected.ShouldNotBeNull();
            collectionProjected.Users1.ShouldNotBeNull();
            collectionProjected.Users1.Count.ShouldBe(2);
            collectionProjected.Users1.All(u => u.Name == TestName).ShouldBeTrue();

            collectionProjected.Users2.ShouldNotBeNull();
            collectionProjected.Users2.Count.ShouldBe(2);
            collectionProjected.Users2.All(u => u.Email == TestEmail).ShouldBeTrue();
        }
    }
}
