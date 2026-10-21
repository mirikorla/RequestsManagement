/** Mirrors BulkItemDto: one request in a bulk update, with the version the client last saw. */
export interface BulkItemDto {
  id: number;
  rowVersion: string;
}
