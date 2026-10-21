import type { RequestStatus } from '../enums/request-status';

/**
 * Same workflow as StatusTransitions.cs. Used only to offer sensible options in the UI;
 * the server is the authority and answers 422 for a transition it does not allow.
 */
export const ALLOWED_TRANSITIONS: Record<RequestStatus, RequestStatus[]> = {
  New: ['InProgress', 'Waiting'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: ['InProgress'],
};
