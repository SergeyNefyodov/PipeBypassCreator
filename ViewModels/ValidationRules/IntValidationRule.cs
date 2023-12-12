using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace PipeBypassCreator.ViewModels.ValidationRules
{
    public class IntValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var expression = (BindingExpression)value;
            var textBox = (TextBox)expression.Target;
            var text = textBox.Text;
            if (text == null || string.IsNullOrWhiteSpace(text))
            {
                return ValidationResult.ValidResult;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return ValidationResult.ValidResult;
            }

            return new ValidationResult(false, "Необходимо ввести целое число");
        }
    }
}
