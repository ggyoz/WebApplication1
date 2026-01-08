using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;

namespace CSR.Filters
{

    /// validation 처리하는 필터 어노테이션만 추가해서 사용할 수 있음
    public class AsyncValidationFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {            
            var model = context.ActionArguments.Values.FirstOrDefault(arg => 
                arg != null && 
                arg.GetType().IsClass && 
                !arg.GetType().IsPrimitive &&
                arg.GetType().Assembly == typeof(Program).Assembly);

            if (model == null)
            {
                await next();
                return;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());            
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator == null)
            {
                await next();
                return;
            }
            
            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(model));

            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(context.ModelState);

                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    context.Result = controller.View(model);
                }
                else
                {
                    context.Result = new BadRequestObjectResult(context.ModelState);
                }
                return;
            }
            
            await next(); 
        }
    }
}
