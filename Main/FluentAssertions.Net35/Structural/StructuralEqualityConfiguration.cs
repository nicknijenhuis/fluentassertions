using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions.Common;
using System.Linq;

namespace FluentAssertions.Structural
{
    public class StructuralEqualityConfiguration<TSubject> : IStructuralEqualityConfiguration
    {
        #region Private Definitions

        private readonly List<ISelectionRule> selectionRules = new List<ISelectionRule>();
        private readonly List<IMatchingRule> matchingRules = new List<IMatchingRule>();
        private readonly List<IAssertionRule> assertionRules = new List<IAssertionRule>();
        private CyclicReferenceHandling cyclicReferenceHandling = CyclicReferenceHandling.ThrowException;

        #endregion

        private StructuralEqualityConfiguration()
        {
            AddRule(new MustMatchByNameRule());

            OverrideAssertionFor<string>(
                ctx => ctx.Subject.Should().Be(ctx.Expectation, ctx.Reason, ctx.ReasonArgs));

            OverrideAssertionFor<DateTime>(
                ctx => ctx.Subject.Should().Be(ctx.Expectation, ctx.Reason, ctx.ReasonArgs));
        }

        public static StructuralEqualityConfiguration<TSubject> Default
        {
            get
            {
                var config = new StructuralEqualityConfiguration<TSubject>();
                config.Recursive();
                config.IncludeAllDeclaredProperties();

                return config;
            }
        }
        
        public static StructuralEqualityConfiguration<TSubject> Empty
        {
            get
            {
                return new StructuralEqualityConfiguration<TSubject>();
            }
        }

        public IEnumerable<ISelectionRule> SelectionRules
        {
            get { return selectionRules; }
        }

        public IEnumerable<IMatchingRule> MatchingRules
        {
            get { return matchingRules; }
        }

        public IEnumerable<IAssertionRule> AssertionRules
        {
            get { return assertionRules; }
        }

        public bool Recurse { get; set; }

        public CyclicReferenceHandling CyclicReferenceHandling
        {
            get { return cyclicReferenceHandling; }
        }

        public StructuralEqualityConfiguration<TSubject> IncludeAllDeclaredProperties()
        {
            ClearAllSelectionRules();
            AddRule(new AllDeclaredPublicPropertiesSelectionRule());
            return this;
        }

        public StructuralEqualityConfiguration<TSubject> IncludeAllRuntimeProperties()
        {
            ClearAllSelectionRules();
            AddRule(new AllRuntimePublicPropertiesSelectionRule());
            return this;
        }

        /// <summary>
        /// Tries to match the properties of the subject with equally named properties on the expectation. Ignores those 
        /// properties that don't exist on the expectation.
        /// </summary>
        public StructuralEqualityConfiguration<TSubject> TryMatchByName()
        {
            ClearAllMatchingRules();
            matchingRules.Add(new TryMatchByNameRule());
            return this;
        }

        /// <summary>
        /// Requires the expectation to have properties which are equally named to properties on the subject.
        /// </summary>
        /// <returns></returns>
        public StructuralEqualityConfiguration<TSubject> MustMatchByName()
        {
            ClearAllMatchingRules();
            matchingRules.Add(new MustMatchByNameRule());
            return this;
        }

        public StructuralEqualityConfiguration<TSubject> Recursive()
        {
            Recurse = true;
            return this;
        }

        public StructuralEqualityConfiguration<TSubject> IgnoreCyclicReferences()
        {
            cyclicReferenceHandling = CyclicReferenceHandling.Ignore;
            return this;
        }

        /// <summary>
        /// Excludes the specified property from the equality assertion.
        /// </summary>
        public StructuralEqualityConfiguration<TSubject> Exclude(Expression<Func<TSubject, object>> propertyExpression)
        {
            string propertyPath = propertyExpression.GetPropertyPath();

            AddRule(new ExcludePropertyByPathSelectionRule(propertyPath));
            return this;
        }

        /// <summary>
        /// Includes the specified property in the equality check.
        /// </summary>
        /// <remarks>
        /// This overrides the default behavior of including all declared properties.
        /// </remarks>
        public StructuralEqualityConfiguration<TSubject> Include(Expression<Func<TSubject, object>> propertyExpression)
        {
            RemoveSelectionRule<AllDeclaredPublicPropertiesSelectionRule>();
            RemoveSelectionRule<AllRuntimePublicPropertiesSelectionRule>();

            AddRule(new IncludePropertySelectionRule(propertyExpression.GetPropertyInfo()));
            return this;
        }

        private void RemoveSelectionRule<T>() where T : ISelectionRule
        {
            foreach (var selectionRule in selectionRules.OfType<T>().ToArray())
            {
                selectionRules.Remove(selectionRule);
            }
        }

        public StructuralEqualityConfiguration<TSubject> OverrideAssertionFor<TPropertyType>(Action<AssertionContext<TPropertyType>> action)
        {
            assertionRules.Insert(0, new AssertionRule<TPropertyType>(
                pi => pi.PropertyType.IsSameOrInherits(typeof(TPropertyType)), action));

            return this;
        }

        public StructuralEqualityConfiguration<TSubject> OverrideAssertion<TPropertyType>(Func<PropertyInfo, bool> predicate, Action<AssertionContext<TPropertyType>> action)
        {
            assertionRules.Insert(0, new AssertionRule<TPropertyType>(predicate, action));
            return this;
        }

        public void ClearAllSelectionRules()
        {
            selectionRules.Clear();
        }

        public void ClearAllMatchingRules()
        {
            matchingRules.Clear();
        }

        public StructuralEqualityConfiguration<TSubject> AddRule(ISelectionRule selectionRule)
        {
            selectionRules.Add(selectionRule);
            return this;
        }

        public StructuralEqualityConfiguration<TSubject> AddRule(IMatchingRule matchingRule)
        {
            matchingRules.Add(matchingRule);
            return this;
        }
    }
}