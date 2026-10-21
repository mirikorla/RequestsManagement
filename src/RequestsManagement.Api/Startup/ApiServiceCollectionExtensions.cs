using System.Globalization;
using System.Text.Json.Serialization;
using FluentValidation;
using RequestsManagement.Api.ErrorHandling;
using RequestsManagement.Api.Filters;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;
using RequestsManagement.Core.Validation;

namespace RequestsManagement.Api.Startup;

/// <summary>Registers the HTTP layer: controllers, JSON, errors as ProblemDetails, validation, OpenAPI.</summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
                // An unparsable body is reported once, by the JSON error, not also as "the request field is required".
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // ProblemDetails (written by the exception handler) uses these options, not the MVC ones above.
        // Without this, enums inside error bodies ("current" on 409, "allowedTransitions" on 422) would be numbers.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();

        AddValidators(services);
        return services;
    }

    private static void AddValidators(IServiceCollection services)
    {
        // Same language for every error message, regardless of the server's OS culture (model binding errors are English).
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;

        // Validators are stateless, so a single instance is shared.
        services.AddSingleton<IValidator<RequestQueryDto>, RequestQueryDtoValidator>();
        services.AddSingleton<IValidator<UpdateStatusDto>, UpdateStatusDtoValidator>();
        services.AddSingleton<IValidator<BulkUpdateStatusDto>, BulkUpdateStatusDtoValidator>();
    }
}
