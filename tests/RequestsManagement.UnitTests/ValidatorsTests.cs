using RequestsManagement.Core.DTOs;
using RequestsManagement.Core.DTOs.Bulk;
using RequestsManagement.Core.Enums;
using RequestsManagement.Core.Validation;

namespace RequestsManagement.UnitTests;

public class ValidatorsTests
{
    private const string ValidRowVersion = "AAAAAAAAB9E="; // 8 bytes, as SQL Server rowversion

    private readonly RequestQueryDtoValidator _query = new();
    private readonly BulkUpdateStatusDtoValidator _bulk = new();
    private readonly UpdateStatusDtoValidator _update = new();

    [Fact]
    public void Default_query_is_valid() => Assert.True(_query.Validate(new RequestQueryDto()).IsValid);

    [Theory]
    [InlineData(0, 20, nameof(RequestQueryDto.Page))]
    [InlineData(1, 0, nameof(RequestQueryDto.PageSize))]
    [InlineData(1, RequestQueryDto.MaxPageSize + 1, nameof(RequestQueryDto.PageSize))]
    public void Out_of_range_paging_is_rejected(int page, int pageSize, string property)
    {
        var result = _query.Validate(new RequestQueryDto { Page = page, PageSize = pageSize });

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void Reversed_date_range_is_rejected()
    {
        var result = _query.Validate(new RequestQueryDto
        {
            CreatedFrom = new DateTime(2026, 2, 1),
            CreatedTo = new DateTime(2026, 1, 1)
        });

        Assert.Contains(result.Errors, e => e.PropertyName == "CreatedFrom");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64!")]
    [InlineData("AAAA")] // valid base64 but only 3 bytes
    public void Update_with_malformed_row_version_is_rejected(string rowVersion)
    {
        var result = _update.Validate(new UpdateStatusDto(RequestStatus.InProgress, rowVersion, "dana"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateStatusDto.RowVersion));
    }

    [Fact]
    public void Update_with_unknown_status_is_rejected()
    {
        var result = _update.Validate(new UpdateStatusDto((RequestStatus)42, ValidRowVersion, "dana"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateStatusDto.Status));
    }

    [Fact]
    public void Bulk_over_the_limit_is_rejected()
    {
        var items = Enumerable.Range(1, BulkUpdateStatusDto.MaxItems + 1).Select(i => new BulkItemDto(i, ValidRowVersion)).ToList();

        var result = _bulk.Validate(new BulkUpdateStatusDto(RequestStatus.InProgress, "dana", items));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BulkUpdateStatusDto.Items));
    }

    [Fact]
    public void Bulk_with_duplicate_ids_is_rejected()
    {
        var items = new[] { new BulkItemDto(7, ValidRowVersion), new BulkItemDto(7, ValidRowVersion) };

        var result = _bulk.Validate(new BulkUpdateStatusDto(RequestStatus.InProgress, "dana", items));

        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("only once"));
    }
}
