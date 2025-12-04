using RabbitMQ.Client;

namespace LayeredArchitecture.Infrastructure.Messaging.RabbitMQ;

public static class RabbitMQTopology
{
    public static void ConfigureTopology(IModel channel)
    {
        // Declare topic exchanges for different event types
        channel.ExchangeDeclare(
            exchange: "orders-events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        channel.ExchangeDeclare(
            exchange: "products-events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        channel.ExchangeDeclare(
            exchange: "payments-events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Declare dead letter exchange for failed messages
        channel.ExchangeDeclare(
            exchange: "dead-letter-exchange",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

        // Declare queues for order events
        channel.QueueDeclare(
            queue: "order-processing-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead-letter-exchange" },
                { "x-dead-letter-routing-key", "dead-letter.order-processing" },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            });

        channel.QueueDeclare(
            queue: "order-notifications-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead-letter-exchange" },
                { "x-dead-letter-routing-key", "dead-letter.order-notifications" },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            });

        // Declare queues for product events
        channel.QueueDeclare(
            queue: "product-inventory-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead-letter-exchange" },
                { "x-dead-letter-routing-key", "dead-letter.product-inventory" },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            });

        channel.QueueDeclare(
            queue: "product-search-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead-letter-exchange" },
                { "x-dead-letter-routing-key", "dead-letter.product-search" },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            });

        // Declare queues for payment events
        channel.QueueDeclare(
            queue: "payment-processing-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead-letter-exchange" },
                { "x-dead-letter-routing-key", "dead-letter.payment-processing" },
                { "x-message-ttl", 300000 } // 5 minutes TTL
            });

        // Declare dead letter queue
        channel.QueueDeclare(
            queue: "dead-letter-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queues to exchanges with routing keys
        // Order processing bindings
        channel.QueueBind(
            queue: "order-processing-queue",
            exchange: "orders-events",
            routingKey: "order.created",
            arguments: null);

        channel.QueueBind(
            queue: "order-processing-queue",
            exchange: "orders-events",
            routingKey: "order.updated",
            arguments: null);

        channel.QueueBind(
            queue: "order-notifications-queue",
            exchange: "orders-events",
            routingKey: "order.created",
            arguments: null);

        channel.QueueBind(
            queue: "order-notifications-queue",
            exchange: "orders-events",
            routingKey: "order.cancelled",
            arguments: null);

        // Product inventory bindings
        channel.QueueBind(
            queue: "product-inventory-queue",
            exchange: "products-events",
            routingKey: "product.created",
            arguments: null);

        channel.QueueBind(
            queue: "product-inventory-queue",
            exchange: "products-events",
            routingKey: "product.updated",
            arguments: null);

        channel.QueueBind(
            queue: "product-inventory-queue",
            exchange: "products-events",
            routingKey: "product.stock.updated",
            arguments: null);

        // Product search bindings
        channel.QueueBind(
            queue: "product-search-queue",
            exchange: "products-events",
            routingKey: "product.created",
            arguments: null);

        channel.QueueBind(
            queue: "product-search-queue",
            exchange: "products-events",
            routingKey: "product.updated",
            arguments: null);

        // Payment processing bindings
        channel.QueueBind(
            queue: "payment-processing-queue",
            exchange: "payments-events",
            routingKey: "payment.processed",
            arguments: null);

        channel.QueueBind(
            queue: "payment-processing-queue",
            exchange: "payments-events",
            routingKey: "payment.failed",
            arguments: null);

        // Dead letter queue binding
        channel.QueueBind(
            queue: "dead-letter-queue",
            exchange: "dead-letter-exchange",
            routingKey: "dead-letter.#",
            arguments: null);
    }
}