import { HttpErrorResponse } from '@angular/common/http';

/** Turns an HTTP error (a ProblemDetails body from the API) into a message suitable for the UI. */
export function describeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'השרת אינו זמין. בדקו את החיבור ונסו שוב.';
    const problem = error.error as {
      title?: string;
      detail?: string;
      errors?: Record<string, string[]>;
    } | null;
    if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
    return problem?.detail ?? problem?.title ?? `שגיאה ${error.status}`;
  }
  return 'אירעה שגיאה לא צפויה.';
}
