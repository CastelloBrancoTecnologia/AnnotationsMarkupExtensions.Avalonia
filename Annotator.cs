using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup;
using Avalonia.Styling;
using Avalonia.DesignerSupport;
using Material.Styles.Assists;

using CBT.DataAnnotations;
using System.Security.AccessControl;
using Avalonia.Controls.Primitives.PopupPositioning;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.ConstrainedExecution;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;

namespace CastelloBranco.AnnotationsMarkupExtensions.Avalonia;

public static class Annotator
{
    private static T? GetCustomAttribute<T>(PropertyInfo? prop, FieldInfo? fi) where T : Attribute
    {
        T? attr = null;

        if (prop != null)
        {
            attr = prop.GetCustomAttributes<T>(true).FirstOrDefault() as T;
        }

        if (attr == null && fi != null)
        {
            attr = fi.GetCustomAttributes<T>(true).FirstOrDefault() as T;
        }

        return attr;
    }

    private static void BindElement (Control element, AvaloniaProperty property, string path, BindingMode mode = BindingMode.TwoWay, object? source=null )
    {
        Binding bnd = new()
        {
            Mode = mode,
            Source = source ?? element.DataContext,
            Path = path
        };

        element.Bind(property, bnd);
    }

    public static readonly AttachedProperty<string> PropertyNameProperty =
        AvaloniaProperty.RegisterAttached<TextBox, Control, string>("PropertyName");

    public static string GetPropertyName(Control element)
    {
        return element.GetValue(PropertyNameProperty);
    }

    public static void SetPropertyName(Control element, string propertyNamevalue)
    {
        //
        // Bind element
        //
        
        element.SetValue(PropertyNameProperty, propertyNamevalue);

        //
        // Anotate DisplayAttribute
        //
        // Put name in label of textbox, Description in Tooltip, Prompt in Whatermark
        // 

        Type type = (element.DataContext?.GetType()) ?? throw new Exception($"DataContext invalid for Annotation Markup !");

        object obj = element.DataContext;

        if (propertyNamevalue.Contains('.'))
        {
            string[] properties_names = propertyNamevalue.Split('.');

            propertyNamevalue = properties_names.Last();

            Array.Resize(ref properties_names, properties_names.Length - 1);

            foreach (string temp_propname in properties_names)
            {
                PropertyInfo temp_prop = type?.GetProperty(temp_propname) ?? throw new Exception($"{temp_propname} invalid for Annotation Markup !"); ;

                obj  = temp_prop.GetValue(obj) ?? throw new Exception($"{temp_propname} is null and invalid for Annotation Markup !");

                type = obj.GetType();
            }
        }

        PropertyInfo? prop = type.GetProperty(propertyNamevalue) ?? throw new Exception($"PropertyName '{propertyNamevalue}' invalid for Annotation Markup !"); // prop_name);

        string fieldname = char.ToLower(propertyNamevalue[0]).ToString();

        if (propertyNamevalue.Length > 1)
        {
            fieldname += propertyNamevalue[1..];
        }

        FieldInfo? fi = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);

        DisplayAttribute? attr_desc = GetCustomAttribute<DisplayAttribute>(prop, fi);

        PropertyInfo? pi_res = null;

        object? ResourceReference = null;

        if (attr_desc != null && attr_desc.Name != null && attr_desc.ResourceType != null)
        {
            pi_res = attr_desc.ResourceType.GetProperty("Resources", BindingFlags.Public | BindingFlags.Static);

            if (pi_res != null)
            {
                ResourceReference = pi_res.GetValue(null);

                if (ResourceReference == null)
                {
                    ResourceReference = attr_desc.ResourceType;
                }
            }
            else
            {
                ResourceReference = attr_desc.ResourceType;
            }
        }

        if (element is TextBox textBox)
        {
            BindElement(textBox, TextBox.TextProperty, propertyNamevalue, BindingMode.TwoWay, obj);

            if (attr_desc != null)
            {
                if (attr_desc.Name != null)
                {
                    if (attr_desc.ResourceType != null && ResourceReference != null)
                    {
                        BindElement(textBox, TextFieldAssist.LabelProperty, attr_desc.Name, BindingMode.OneWay, ResourceReference);
                    }
                    else
                    {
                        TextFieldAssist.SetLabel(textBox, attr_desc.Name);
                    }

                    textBox.UseFloatingWatermark = true;
                }
            }

            MaskAttribute? attr_mask = GetCustomAttribute<MaskAttribute>(prop, fi);

            if (attr_mask != null && attr_mask.Mask != null)
            {
                textBox.Watermark = attr_mask.Mask;
                textBox.UseFloatingWatermark = true;

                if (element is MaskedTextBox maskedTextBox)
                {
                    maskedTextBox.Mask = attr_mask.Mask;
                }
            }
            else
            {
                if (attr_desc != null && attr_desc.Prompt != null)
                {
                    textBox.UseFloatingWatermark = true;

                    if (attr_desc.ResourceType != null && ResourceReference != null)
                    {
                        BindElement(textBox, TextBox.WatermarkProperty, attr_desc.Prompt, BindingMode.OneWay, ResourceReference);
                    }
                    else
                    {
                        textBox.Watermark = attr_desc.Prompt;
                    }
                }

                EmailAddressAttribute? attr_email = GetCustomAttribute<EmailAddressAttribute>(prop, fi);
                PhoneAttribute? attr_phone = GetCustomAttribute<PhoneAttribute>(prop, fi);
                CreditCardAttribute? attr_credit_card = GetCustomAttribute<CreditCardAttribute>(prop, fi);
                DataTypeAttribute? data_type_attr = GetCustomAttribute<DataTypeAttribute>(prop, fi);

                if (element is MaskedTextBox maskedTextBox)
                {
                    if (attr_phone != null)
                    {
                        maskedTextBox.Mask = "(99) (99) 99999-9999";
                    }


                    if (attr_credit_card != null)
                    {
                        maskedTextBox.Mask = "9999.9999.9999.9999";
                    }

                    if (data_type_attr != null)
                    {
                        CultureInfo Cultura = System.Threading.Thread.CurrentThread.CurrentCulture;

                        maskedTextBox.Mask = data_type_attr.DataType switch
                        {
                            DataType.Currency => new string('9', 12 - Cultura.NumberFormat.CurrencyDecimalDigits) +
                                                      Cultura.NumberFormat.CurrencyDecimalSeparator +
                                                      new string('9', Cultura.NumberFormat.CurrencyDecimalDigits),
                            DataType.PhoneNumber => "(99) (99) 99999-99999",
                            DataType.CreditCard => "9999.9999.9999.9999",
                            DataType.Date => "99/99/9999",
                            DataType.DateTime => "99/99/9999 - 99:99:99",
                            DataType.PostalCode => "99999-999",
                            DataType.Time => "99:99:99",
                            _ => maskedTextBox.Mask
                        };
                    }
                }
                else
                {
                    if (attr_email != null)
                    {
                        textBox.UseFloatingWatermark = true;
                        textBox.Watermark = "exemplo@emailqualquer.com.br";
                    }

                    UrlAttribute? attr_url = GetCustomAttribute<UrlAttribute>(prop, fi);

                    if (attr_url != null)
                    {
                        textBox.UseFloatingWatermark = true;
                        textBox.Watermark = "https://www.enderecoqualquer.com.br/url";
                    }

                    if (attr_phone != null)
                    {
                        textBox.UseFloatingWatermark = true;
                        textBox.Watermark = "(99) (99) 99999-9999";
                    }

                    if (attr_credit_card != null)
                    {
                        textBox.UseFloatingWatermark = true;
                        textBox.Watermark = "9999.9999.9999.9999";
                    }

                    if (data_type_attr != null)
                    {
                        textBox.UseFloatingWatermark = true;

                        CultureInfo Cultura = System.Threading.Thread.CurrentThread.CurrentCulture;

                        textBox.Watermark = data_type_attr.DataType switch
                        {
                            DataType.Currency => new string('9', 12 - Cultura.NumberFormat.CurrencyDecimalDigits) +
                                                      Cultura.NumberFormat.CurrencyDecimalSeparator +
                                                      new string('9', Cultura.NumberFormat.CurrencyDecimalDigits),
                            DataType.PhoneNumber => "(99) (99) 99999-99999",
                            DataType.CreditCard => "9999.9999.9999.9999",
                            DataType.Date => "99/99/9999",
                            DataType.DateTime => "99/99/9999 - 99:99:99",
                            DataType.PostalCode => "99999-999",
                            DataType.Time => "99:99:99",
                            _ => textBox.Watermark
                        };
                    }
                }
            }

            DataTypeAttribute? attr_data_type = GetCustomAttribute<DataTypeAttribute>(prop, fi);

            if (attr_data_type?.DataType == DataType.Password)
            {
                textBox.PasswordChar = '*';

                textBox.Classes.Remove("clearButton");

                textBox.Classes.Add("revealPasswordButton");
            }
            else
            {
                textBox.Classes.Add("clearButton");
            }

            LowerCaseAttribute? attr_lowercase = GetCustomAttribute<LowerCaseAttribute>(prop, fi);

            if (attr_lowercase != null)
            {
                // TextBox.ChacaracterCasing = ChacaracterCasing.Lower;
            }

            UpperCaseAttribute? attr_uppercase = GetCustomAttribute<UpperCaseAttribute>(prop, fi);

            if (attr_uppercase != null)
            {
                // TextBox.ChacaracterCasing = ChacaracterCasing.Upper;
            }

            EditableAttribute? attr_editable = GetCustomAttribute<EditableAttribute>(prop, fi);

            if (attr_editable != null && attr_editable.AllowEdit == false)
            {
                textBox.IsReadOnly = true;
            }

            ReadOnlyAttribute? attr_readonly = GetCustomAttribute<ReadOnlyAttribute>(prop, fi);

            if (attr_readonly != null && attr_readonly.IsReadOnly == true)
            {
                textBox.IsReadOnly = true;
            }

            StringLengthAttribute? attr_strlen = GetCustomAttribute<StringLengthAttribute>(prop, fi);

            if (attr_strlen != null && attr_strlen.MaximumLength > 0)
            {
                textBox.MaxLength = attr_strlen.MaximumLength;
            }

            MaxLengthAttribute? attr_maxlen = GetCustomAttribute<MaxLengthAttribute>(prop, fi);

            if (attr_maxlen != null && attr_maxlen.Length > 0)
            {
                textBox.MaxLength = attr_maxlen.Length;
            }
        }

        if (attr_desc != null  && attr_desc.Description != null)
        {
            if (attr_desc.ResourceType != null && ResourceReference != null)
            {
                BindElement(element, ToolTip.TipProperty, attr_desc.Description, BindingMode.OneWay, ResourceReference);
            }
            else
            {
                element.SetValue(ToolTip.TipProperty, attr_desc.Description);
            }
        }

        RangeAttribute? attr_range = GetCustomAttribute<RangeAttribute>(prop, fi);

        if (attr_range != null && attr_range.Maximum != null && attr_range.Minimum != null)
        {
            if (element is Slider slider)
            {
                slider.Minimum = Convert.ToDouble(attr_range.Minimum);
                slider.Maximum = Convert.ToDouble(attr_range.Maximum);  
            }

            if (element is ProgressBar progressBar)
            {
                progressBar.Minimum = Convert.ToDouble(attr_range.Minimum);
                progressBar.Maximum = Convert.ToDouble(attr_range.Maximum);
            }

            if (element is NumericUpDown numericUpDown)
            {
                numericUpDown.Minimum = Convert.ToDecimal(attr_range.Minimum);
                numericUpDown.Maximum = Convert.ToDecimal(attr_range.Maximum);
            }
        }

        if (element is CheckBox || element is RadioButton)
        {
            BindElement(element, element is CheckBox ? CheckBox.IsCheckedProperty : RadioButton.IsCheckedProperty, propertyNamevalue, BindingMode.TwoWay, obj);

            if (attr_desc != null && attr_desc.Name != null)
            {
                TextBlock tb = new();

                if (attr_desc.ResourceType != null && ResourceReference != null)
                {
                    BindElement(tb, TextBlock.TextProperty, attr_desc.Name, BindingMode.OneWay, ResourceReference);
                }
                else
                {
                    tb.Text = attr_desc.Name;
                }

                ((ContentControl)element).Content = tb;
            }
        }
    }
}


