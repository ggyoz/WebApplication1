using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CSR.Data
{
    /// <summary>
    /// DevExtreme DataGrid의 필터 JSON을 SQL WHERE 조건절로 변환하는 유틸리티 클래스입니다.
    /// </summary>
    public static class DevExtremeFilterConverter
    {
        /// <summary>
        /// DevExtreme 필터 JSON 문자열을 SQL 조건절로 변환합니다.
        /// </summary>
        /// <param name="filterJson">그리드로부터 전달된 filter 파라미터</param>
        /// <returns>SQL WHERE 조건 (예: "(COL1 = 'Val' AND COL2 > 10)")</returns>
        public static string ToSql(string filterJson)
        {
            if (string.IsNullOrWhiteSpace(filterJson) || filterJson == "null")
                return string.Empty;

            try
            {
                JToken token = JToken.Parse(filterJson);
                return ParseToken(token);
            }
            catch (Exception ex)
            {
                // 파싱 오류 발생 시 빈 문자열 반환 (필터 무시)
                System.Diagnostics.Debug.WriteLine($"Filter Parsing Error: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ParseToken(JToken token)
        {
            if (token.Type != JTokenType.Array) return string.Empty;

            var filterArray = (JArray)token;

            // 1. 단순 필터 형태: ["field", "op", "value"]
            if (filterArray.Count == 3 && filterArray[1].Type == JTokenType.String && IsOperator(filterArray[1].ToString()))
            {
                string field = filterArray[0].ToString();
                string op = filterArray[1].ToString();
                JToken value = filterArray[2];

                return BuildCondition(field, op, value);
            }

            // 2. 복합 필터 형태: [ [filter1], "and", [filter2], ... ] 또는 ["!", [filter]]
            var sb = new StringBuilder();
            sb.Append("(");

            for (int i = 0; i < filterArray.Count; i++)
            {
                var item = filterArray[i];

                if (item.Type == JTokenType.Array)
                {
                    sb.Append(ParseToken(item));
                }
                else if (item.Type == JTokenType.String)
                {
                    string logicOp = item.ToString().ToLower();
                    switch (logicOp)
                    {
                        case "and": sb.Append(" AND "); break;
                        case "or": sb.Append(" OR "); break;
                        case "!": sb.Append(" NOT "); break;
                    }
                }
            }

            sb.Append(")");
            return sb.ToString();
        }

        private static bool IsOperator(string op)
        {
            string[] operators = { "=", "<>", ">", ">=", "<", "<=", "contains", "notcontains", "startswith", "endswith" };
            return operators.Contains(op.ToLower());
        }

        private static string BuildCondition(string field, string op, JToken valueToken)
        {
            string value = valueToken?.ToString() ?? string.Empty;
            
            // SQL Injection 방지를 위한 기본 이스케이프 (작은 따옴표 처리)
            value = value.Replace("'", "''");

            // 날짜 컬럼인 경우 처리 (컬럼명에 'date' 또는 'dt'가 포함되는 경우)
            string lowerField = field.ToLower();
            if (lowerField.Contains("date") || lowerField.EndsWith("dt"))
            {
                return BuildDateCondition(field, op, value);
            }

            switch (op.ToLower())
            {
                case "=": return $"{field} = '{value}'";
                case "<>": return $"{field} != '{value}'";
                case ">": return $"{field} > '{value}'";
                case ">=": return $"{field} >= '{value}'";
                case "<": return $"{field} < '{value}'";
                case "<=": return $"{field} <= '{value}'";
                case "contains": return $"{field} LIKE '%{value}%'";
                case "notcontains": return $"{field} NOT LIKE '%{value}%'";
                case "startswith": return $"{field} LIKE '{value}%'";
                case "endswith": return $"{field} LIKE '%{value}'";
                default: return "1=1";
            }
        }

        private static string BuildDateCondition(string field, string op, string value)
        {
            if (string.IsNullOrEmpty(value)) return "1=1";

            // DevExtreme 날짜 형식 "2024-03-30T00:00:00"에서 날짜 부분만 추출
            string datePart = value.Contains("T") ? value.Split('T')[0] : value;
            
            // Oracle 11g 호환 TO_DATE 포맷
            string oracleDate = $"TO_DATE('{datePart}', 'YYYY-MM-DD')";

            switch (op.ToLower())
            {
                case "=": return $"{field} = {oracleDate}";
                case "<>": return $"{field} != {oracleDate}";
                case ">": return $"{field} > {oracleDate}";
                case ">=": return $"{field} >= {oracleDate}";
                case "<": return $"{field} < {oracleDate}";
                case "<=": return $"{field} <= {oracleDate}";
                default: return "1=1";
            }
        }
    }
}
