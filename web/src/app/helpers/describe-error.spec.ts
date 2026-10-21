import { HttpErrorResponse } from '@angular/common/http';
import { describeError } from './describe-error';

describe('describeError', () => {
  it('explains that the server is unreachable (status 0)', () => {
    const error = new HttpErrorResponse({ status: 0 });

    expect(describeError(error)).toContain('השרת אינו זמין');
  });

  it('joins all field messages of a 400 ValidationProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'One or more validation errors occurred.', errors: { PageSize: ['too big'], Page: ['too small'] } },
    });

    expect(describeError(error)).toBe('too big too small');
  });

  it('uses the ProblemDetails detail when there are no field errors', () => {
    const error = new HttpErrorResponse({ status: 404, error: { title: 'Not found', detail: "Request '9' was not found." } });

    expect(describeError(error)).toBe("Request '9' was not found.");
  });
});
