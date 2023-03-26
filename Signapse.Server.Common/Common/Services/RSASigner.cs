using Microsoft.IdentityModel.Tokens;
using Signapse.BlockChain;
using Signapse.Data;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Signapse.Services
{
    public class RSASigner
    {
        readonly ISecureStorage storage;
        readonly RSAParameters rsaParameters;
        readonly JsonSerializerFactory jsonFactory;

        public RSAParameters PublicKey { get; }

        public RSASigner(ISecureStorage storage, JsonSerializerFactory jsonFactory)
        {
            this.storage = storage;
            this.jsonFactory = jsonFactory;

            using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            var xml = storage.ReadFile("signature_keys.xml").Result;
            if (!string.IsNullOrEmpty(xml))
            {
                RSA.FromXmlString(xml);
            }

            rsaParameters = RSA.ExportParameters(true);
            this.PublicKey = RSA.ExportParameters(false);
        }

        //public bool Verify<T>(RSAParameters publicParameters, T obj, string signature)
        //    => Verify(publicParameters, obj.Serialize(), signature);

        public bool Verify(RSAParameters publicParameters, IBlock block, string signature)
            => Verify(publicParameters, jsonFactory.Serialize(block.Transaction), signature);

        public bool Verify(RSAParameters publicParameters, string content, string signature)
        {
            using SHA256 alg = SHA256.Create();

            byte[] data = Encoding.ASCII.GetBytes(content);
            byte[] hash = alg.ComputeHash(data);
            byte[] signedHash = Convert.FromHexString(signature);

            // Verify signature
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(publicParameters);

                RSAPKCS1SignatureDeformatter rsaDeformatter = new(rsa);
                rsaDeformatter.SetHashAlgorithm(nameof(SHA256));

                return rsaDeformatter.VerifySignature(hash, signedHash);
            }
        }

        //public string Sign<T>(T obj) => Sign(obj.Serialize());
        public string Sign(IBlock block) => Sign(jsonFactory.Serialize(block.Transaction));

        public string Sign(string content)
        {
            using SHA256 alg = SHA256.Create();

            byte[] data = Encoding.ASCII.GetBytes(content);
            byte[] hash = alg.ComputeHash(data);

            // Generate signature
            using RSA rsa = RSA.Create();
            rsa.ImportParameters(rsaParameters);

            RSAPKCS1SignatureFormatter rsaFormatter = new(rsa);
            rsaFormatter.SetHashAlgorithm(nameof(SHA256));

            return Convert.ToHexString(rsaFormatter.CreateSignature(hash));
        }

        public SigningCredentials SigningCredentials => new SigningCredentials(
            new RsaSecurityKey(rsaParameters),
            SecurityAlgorithms.RsaSha256
        );

        /// <summary>
        /// Generate an RSA-signed Javascript Web Token for a specified user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public string GenerateJWT(User user)
        {
            // Create a list of claims for the JWT token
            List<Claim> claims = Claims.CreatePrincipal(user, "cookie")
                .Claims
                .ToList();

            claims.Add(new Claim("sub", user.ID.ToString()));

            // Create the JWT token
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "example1.com",
                audience: "example2.com",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(
                    new RsaSecurityKey(rsaParameters),
                    SecurityAlgorithms.RsaSha256
                )
            );

            // Write the token to a string
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generate an X509Certificate2 certificate
        /// </summary>
        /// <param name="subjectName"></param>
        /// <returns></returns>
        public X509Certificate2 GenerateSelfSignedCertificate(string subjectName)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
        }
    }
}