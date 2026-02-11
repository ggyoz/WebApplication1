using CSR.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CSR.Authorization
{
    public class SameTeamReqAuthorizationHandler : AuthorizationHandler<SameTeamRequirement, ReqInfo>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SameTeamReqAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SameTeamRequirement requirement,
            ReqInfo resource) // 'resource' 매개변수로 검사할 게시글(ReqInfo) 객체가 들어옵니다.
        {
            // HttpContext에 접근하기 위해 필요합니다.
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.CompletedTask;
            }

            // 1. 사용자의 역할(등급)을 가져옵니다.
            string? userRole = context.User.FindFirstValue(ClaimTypes.Role);

            // 2. 'R1' 등급이 아니면, 이 규칙은 적용되지 않으므로 항상 통과시킵니다.
            if (userRole != "R1")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 3. 'R1' 등급인 경우, 세션에서 사용자의 팀 코드를 가져옵니다.
            string? userTeamCd = httpContext.Session.GetString("TeamCd");

            // 4. 게시글의 팀 코드(resource.TeamCd)와 사용자의 팀 코드가 같은지 비교합니다.
            if (!string.IsNullOrEmpty(userTeamCd) && userTeamCd == resource.TEAMCD)
            {
                // 코드가 같으면 권한 부여에 성공합니다.
                context.Succeed(requirement);
            }

            // 성공하지 못하면 아무것도 하지 않고 종료됩니다 (접근 실패).
            return Task.CompletedTask;
        }
    }
}
