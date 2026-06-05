// Arquivo: TecFlow.Core.Interfaces\IDataService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Services
{
    /// <summary>
    /// Contrato genérico para serviços de acesso a dados, suportando operações CRUD
    /// em entidades que herdam de BaseEntity.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Obtém uma entidade pelo seu ID.
        /// </summary>
        /// <typeparam name="TEntity">O tipo da entidade a ser buscada (deve herdar de BaseEntity).</typeparam>
        /// <param name="id">O ID da entidade a ser buscada.</param>
        /// <returns>A entidade encontrada, ou null se não for encontrada.</returns>
        Task<TEntity?> GetByIdAsync<TEntity>(int id) where TEntity : BaseEntity;

        /// <summary>
        /// Obtém todas as entidades de um determinado tipo.
        /// </summary>
        /// <typeparam name="TEntity">O tipo das entidades a serem listadas (deve herdar de BaseEntity).</typeparam>
        /// <returns>Uma coleção de todas as entidades encontradas.</returns>
        Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Adiciona uma nova entidade ao repositório.
        /// </summary>
        /// <typeparam name="TEntity">O tipo da entidade a ser adicionada (deve herdar de BaseEntity).</typeparam>
        /// <param name="entity">A entidade a ser adicionada.</param>
        /// <returns>A entidade adicionada, possivelmente com o ID gerado e campos preenchidos (como CreatedAt).</returns>
        Task<TEntity> AddAsync<TEntity>(TEntity entity) where TEntity : BaseEntity;

        /// <summary>
        /// Atualiza uma entidade existente no repositório.
        /// </summary>
        /// <typeparam name="TEntity">O tipo da entidade a ser atualizada (deve herdar de BaseEntity).</typeparam>
        /// <param name="entity">A entidade com os dados atualizados. Deve possuir um ID válido.</param>
        /// <returns>A entidade atualizada.</returns>
        Task<TEntity> UpdateAsync<TEntity>(TEntity entity) where TEntity : BaseEntity;

        /// <summary>
        /// Exclui uma entidade do repositório pelo seu ID.
        /// </summary>
        /// <typeparam name="TEntity">O tipo da entidade a ser excluída (deve herdar de BaseEntity).</typeparam>
        /// <param name="id">O ID da entidade a ser excluída.</param>
        /// <returns>Uma Task que representa a operação de exclusão.</returns>
        Task DeleteAsync<TEntity>(int id) where TEntity : BaseEntity;
    }
}