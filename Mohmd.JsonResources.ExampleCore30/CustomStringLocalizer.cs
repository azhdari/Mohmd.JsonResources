using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mohmd.JsonResources.ExampleCore30
{
    public class CustomStringLocalizer<T> : IStringLocalizer<T>
    {
        private IStringLocalizerFactory _factory;
        private IStringLocalizer<T> _localizer = null;

        public CustomStringLocalizer(IStringLocalizerFactory factory)
        {
            _factory = factory;
        }

        public LocalizedString this[string name] => GetStringLocalizer()[name];

        public LocalizedString this[string name, params object[] arguments] => GetStringLocalizer()[name, arguments];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return GetStringLocalizer().GetAllStrings(includeParentCultures);
        }

        [Obsolete("This method is obsolete. Use `CurrentCulture` and `CurrentUICulture` instead.")]
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return GetStringLocalizer().WithCulture(culture);
        }

        private IStringLocalizer<T> GetStringLocalizer()
        {
            _localizer ??= _factory.Create(typeof(T)) as IStringLocalizer<T>;
            return _localizer;
        }
    }
}
