﻿using Avalonia.Markup.Xaml;
using System;

namespace L4D2AddonAssistant.MarkupExtensions
{
    public class EnumValues : MarkupExtension
    {
        private Type _enumType;

        public EnumValues(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("The argument isn't a enum type.");
            }

            _enumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(_enumType);
        }
    }
}
