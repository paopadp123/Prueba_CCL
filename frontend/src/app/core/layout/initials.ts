/**
 * Iniciales para el avatar: primera letra de las dos primeras palabras ("Ana María Pérez" → "AM").
 */
export function initialsOf(fullName: string | null | undefined): string {
  return (fullName ?? '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join('');
}
