using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IS.DbCommon.Models.DTO
{
    public static class Mapper
    {
        public static TDtoValue ToDto<TDtoValue, TSerValue>(TSerValue old)
        {
            try
            {
                var dto = GenericCopy<TDtoValue, TSerValue>(old!);
                return dto;
            }
            catch (Exception ex) 
            {
                throw new NotImplementedException($"Mapping from {typeof(TSerValue)} to {typeof(TDtoValue)} was thrown an exeption. \nException message: {ex.Message}");
            }

            
        } 

        private static T1 GenericCopy
        <[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicProperties)]
            T1,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties)]
            T2
        >
        (T2 value)
        {
            var castProps = typeof(T2).GetProperties();
            var targetProps = typeof(T1).GetProperties();

            T1 t1 = Activator.CreateInstance<T1>();

            foreach (var cprop in castProps)
            {
                var targetProp = targetProps.FirstOrDefault(p => p.Name == cprop.Name && p.PropertyType == cprop.PropertyType);
                if (targetProp != null && targetProp.CanWrite)
                {
                    var propValue = cprop.GetValue(value);
                    targetProp.SetValue(t1, propValue);
                }
            }

            return t1;
        }
    }
}
