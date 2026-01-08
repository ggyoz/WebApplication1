using Dapper;
using System;
using System.Data;

namespace CSR.Data
{
    public class BooleanNumericTypeHandler : SqlMapper.TypeHandler<bool>
    {
        public override void SetValue(IDbDataParameter parameter, bool value)
        {
            parameter.Value = value ? 1 : 0;
            parameter.DbType = DbType.Int32;
        }

        public override bool Parse(object value)
        {
            if (value == null || value is DBNull)
            {
                return false;
            }

            // Oracle DB에서 NUMBER 타입은 다양한 숫자 형태(decimal, long, int 등)로 올 수 있으므로,
            // 공통 타입으로 변환하여 비교하는 것이 안전합니다.
            try
            {
                return Convert.ToInt32(value) == 1;
            }
            catch
            {
                // 변환 중 오류 발생 시 기본값 false로 처리
                return false;
            }
        }
    }
}
