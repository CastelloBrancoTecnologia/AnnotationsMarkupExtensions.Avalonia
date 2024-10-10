using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastelloBranco.AnnotationsMarkupExtensions.Avalonia;

public class LocalizeExtension : MarkupExtension
{
    public object? Resource { get; set; }

    public LocalizeExtension(string key)
    {
        this.Key = key;
    }

    public string Key { get; set; }

    public string Context { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Resource == null)
        {
            throw new ArgumentNullException(nameof(Resource));
        }

        var keyToUse = Key;

        if (!string.IsNullOrWhiteSpace(Context))
            keyToUse = $"{Context}/{Key}";

        var binding = new ReflectionBindingExtension($"[{keyToUse}]")
        {
            Mode = BindingMode.OneWay,
            Source = Resource,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
