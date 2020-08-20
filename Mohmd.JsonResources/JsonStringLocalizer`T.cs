using Microsoft.Extensions.Localization;
using Mohmd.JsonResources.Extensions;
using Mohmd.JsonResources.Internal;
using Mohmd.JsonResources.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mohmd.JsonResources
{
    public class JsonStringLocalizer<T> : IStringLocalizer<T>
    {
        #region Fields

        private readonly IJsonResourceProvider _resourceProvider;

        #endregion

        #region Ctors

        public JsonStringLocalizer(string resourceName, JsonLocalizationOptions options, IJsonResourceProviderFactory resourceProviderFactory)
        {
            ResourceName = resourceName;
            _resourceProvider = resourceProviderFactory.Create(resourceName, options);
        }

        #endregion

        #region Properties

        public string ResourceName { get; }

        #endregion

        #region Indexes

        public LocalizedString this[string name]
        {
            get
            {
                string value = _resourceProvider.GetStringSafely(name) ?? name;
                return new LocalizedString(name, value, resourceNotFound: value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string? format = _resourceProvider.GetStringSafely(name);
                string value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        #endregion

        #region Methods

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
