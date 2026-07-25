namespace QuizArena.Core.Entities;

/// <summary>
/// Katmanlar arası veri taşıyan nesneleri işaretler (marker interface).
/// <para>
/// Not: Bu tip projenin ilk hâlinde <c>class</c> olarak tanımlıydı; bu yüzden
/// bir DTO hem <c>IDto</c>'dan hem de başka bir taban sınıftan türeyemiyordu.
/// Arayüze çevrildi — çoklu kalıtım kısıtı ortadan kalktı.
/// </para>
/// </summary>
public interface IDto;
