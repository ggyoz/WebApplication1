using FluentValidation;
using CSR.Models;
using CSR.Services;
using System.Threading;
using System.Threading.Tasks;

namespace CSR.Validators
{
    public class UserValidator : AbstractValidator<User>
    {
        private readonly UserService _userService;

        public UserValidator(UserService userService)
        {
            _userService = userService;

            RuleFor(user => user.UserId)
                .NotEmpty().WithMessage("사용자 ID는 필수입니다.")
                .Length(4, 20).WithMessage("사용자 ID는 4자에서 20자 사이여야 합니다.")
                .MustAsync(BeUniqueUserId).WithMessage("이미 사용 중인 ID입니다.")
                .When(user => user.RegDate == default, ApplyConditionTo.CurrentValidator); // RegDate가 설정되지 않은 새 사용자일 때만 이 규칙을 적용합니다.

            RuleFor(user => user.UserName)
                .NotEmpty().WithMessage("사용자 이름은 필수입니다.");

            // RuleFor(user => user.EmailAddr)
            //     .NotEmpty().WithMessage("이메일 주소는 필수입니다.")
            //     .EmailAddress().WithMessage("올바른 이메일 형식이 아닙니다.");
        }

        private async Task<bool> BeUniqueUserId(string? userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return true; // NotEmpty 규칙이 이 경우를 처리합니다.
            }
            var existingUser = await _userService.GetUserByIdAsync(userId);
            return existingUser == null; // 사용자를 찾지 못하면 ID가 고유한 것이므로 true를 반환합니다.
        }
    }
}
