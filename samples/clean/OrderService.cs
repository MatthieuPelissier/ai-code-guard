using Microsoft.Extensions.Logging;

namespace Demo.Orders;

public interface IOrderRepository
{
    Task<Order?> FindAsync(int orderId, CancellationToken cancellationToken);
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public sealed record Order(int Id, decimal Amount, bool IsPaid);

/// <summary>Service métier : version relue et corrigée d'un code initialement généré par IA.</summary>
public sealed class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> MarkAsPaidAsync(int orderId, CancellationToken cancellationToken)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId), "L'identifiant doit être positif.");
        }

        var order = await _repository.FindAsync(orderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Commande {OrderId} introuvable", orderId);
            return false;
        }

        if (order.IsPaid)
        {
            return true;
        }

        await _repository.SaveAsync(order with { IsPaid = true }, cancellationToken);
        _logger.LogInformation("Commande {OrderId} marquée comme payée", orderId);
        return true;
    }
}
