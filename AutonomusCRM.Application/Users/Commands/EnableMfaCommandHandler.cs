using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using OtpNet;
using System.Text;

namespace AutonomusCRM.Application.Users.Commands;

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, EnableMfaResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public EnableMfaCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<EnableMfaResult> HandleAsync(EnableMfaCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null || user.TenantId != request.TenantId)
            throw new InvalidOperationException("Usuario no encontrado");

        // Generar secreto MFA
        var secret = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secret);

        user.EnableMfa(base32Secret);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        // Generar URL para QR code
        var qrCodeUrl = $"otpauth://totp/AutonomusCRM:{user.Email}?secret={base32Secret}&issuer=AutonomusCRM";

        return new EnableMfaResult(base32Secret, qrCodeUrl);
    }
}

