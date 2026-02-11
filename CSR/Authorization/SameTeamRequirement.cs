using Microsoft.AspNetCore.Authorization;

namespace CSR.Authorization
{
    // 이 클래스는 특정 리소스(게시글)에 대한 '같은 팀' 요구사항을 나타내는 마커입니다.
    public class SameTeamRequirement : IAuthorizationRequirement
    {
    }
}
