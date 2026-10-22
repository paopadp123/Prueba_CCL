using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Domain.Entities;

/// <summary>
/// Producto del catálogo con su stock vigente.
/// El stock solo cambia mediante movimientos registrados en la base de datos.
/// </summary>
public sealed class Product
{
    /// <summary>
    /// Longitud máxima del nombre.
    /// </summary>
    public const int NameMaxLength = 150;

    /// <summary>
    /// Longitud máxima de la descripción.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Precio máximo admitido por la columna NUMERIC(12,2).
    /// </summary>
    public const decimal UnitPriceMax = 9_999_999_999.99m;

    private Product(Sku sku, string name, string? description, decimal unitPrice)
    {
        Sku = sku;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        IsActive = true;
    }

    /// <summary>
    /// Identificador; 0 mientras no se haya guardado.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Código único del producto.
    /// </summary>
    public Sku Sku { get; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Descripción opcional.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Precio unitario, mayor o igual a cero y con máximo dos decimales.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Stock vigente.
    /// </summary>
    public int CurrentStock { get; private set; }

    /// <summary>
    /// Indica si el producto admite movimientos.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Fecha de creación.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Fecha de la última modificación.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Crea un producto nuevo, activo y sin stock.
    /// </summary>
    public static Product Create(Sku sku, string? name, string? description, decimal unitPrice)
    {
        ArgumentNullException.ThrowIfNull(sku);

        return new Product(sku, ValidateName(name), NormalizeDescription(description), ValidatePrice(unitPrice));
    }

    /// <summary>
    /// Reconstruye un producto existente a partir de los datos persistidos.
    /// </summary>
    public static Product Rehydrate(
        long id,
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        int currentStock,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new(Sku.Create(sku), name, description, unitPrice)
        {
            Id = id,
            CurrentStock = currentStock,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

    /// <summary>
    /// Actualiza nombre, descripción y precio; si algún dato es inválido no cambia nada.
    /// </summary>
    public void UpdateDetails(string? name, string? description, decimal unitPrice)
    {
        string validName = ValidateName(name);
        string? validDescription = NormalizeDescription(description);
        decimal validPrice = ValidatePrice(unitPrice);

        Name = validName;
        Description = validDescription;
        UnitPrice = validPrice;
    }

    /// <summary>
    /// Habilita el producto para registrar movimientos.
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Deshabilita el producto; conserva su historial.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Valida de forma anticipada que la salida es posible con el stock conocido.
    /// La validación definitiva la hace el procedimiento almacenado con bloqueo de fila.
    /// </summary>
    public void EnsureCanWithdraw(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);

        if (!IsActive)
        {
            throw new InactiveProductException(Id);
        }

        if (quantity.Value > CurrentStock)
        {
            throw new InsufficientStockException(CurrentStock, quantity.Value);
        }
    }

    private static string ValidateName(string? name)
    {
        string? trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException("El nombre del producto es obligatorio.");
        }

        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainValidationException($"El nombre no puede superar {NameMaxLength} caracteres.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        string? trimmed = description?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= DescriptionMaxLength
            ? trimmed
            : throw new DomainValidationException($"La descripción no puede superar {DescriptionMaxLength} caracteres.");
    }

    private static decimal ValidatePrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new DomainValidationException("El precio no puede ser negativo.");
        }

        if (unitPrice > UnitPriceMax)
        {
            throw new DomainValidationException($"El precio no puede superar {UnitPriceMax:N2}.");
        }

        if (decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new DomainValidationException("El precio admite máximo dos decimales.");
        }

        return unitPrice;
    }
}
