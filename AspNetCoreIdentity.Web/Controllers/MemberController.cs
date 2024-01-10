using AspNetCoreIdentity.Web.Extensions;
using AspNetCoreIdentity.Web.Models;
using AspNetCoreIdentity.Web.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace AspNetCoreIdentity.Web.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IFileProvider _fileProvider;
        AppDbContext _context;

        public MemberController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IFileProvider fileProvider, AppDbContext context = null)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _fileProvider = fileProvider;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var userviewModel = new UserViewModel
            {
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber,
                UserName = currentUser.UserName,
                PhotoUrl = currentUser.Photo

            };
            var userAracList = await _context.AracDurumu.Where(x => x.KullanciId == currentUser.Id).ToListAsync();

            foreach (var item in userAracList)
            {
                var arac = await _context.Arac.FirstOrDefaultAsync(x=>x.Id == item.AracId);
                userviewModel.aracKiralamaList.Add(new UserAracKiralamaViewModel
                {
                    UserId = currentUser.Id,
                    ToplamUcret = item.ToplamUcret,
                    BasTar = item.BaslangicTarihi,
                    BitTar = item.BitisTarihi,
                    Kategori = arac.Kategori,
                    Marka = arac.Marka,
                    Model = arac.Model,
                    Vites = arac.Vites,
                });
            }

            return View(userviewModel);
        }

        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        public IActionResult PasswordChange()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PasswordChange(PasswordChangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity!.Name!);

            var checkOldPassword = await _userManager.CheckPasswordAsync(currentUser, model.PasswordOld);
            if (!checkOldPassword)
            {
                ModelState.AddModelError(string.Empty, "Eski şifreniz yanlış");
                return View();
            }

            var resultChangePassword = await _userManager.ChangePasswordAsync(currentUser, model.PasswordOld, model.PasswordNew);
            if (!resultChangePassword.Succeeded)
            {
                ModelState.AddModelErrorList(resultChangePassword.Errors);
                return View();
            }

            await _userManager.UpdateSecurityStampAsync(currentUser);//Kritik bir field oldugu için baska tarayıcılarda acıksa kapatsın
            await _signInManager.SignOutAsync();
            await _signInManager.PasswordSignInAsync(currentUser, model.PasswordNew, true, false);
            TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirilmiştir.";


            return View();
        }

        public async Task<IActionResult> UserEdit()
        {
            ViewBag.genderList = new SelectList(Enum.GetNames(typeof(Gender)));
            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            var userEditViewModel = new UserEditViewModel
            {
                UserName = currentUser.UserName,
                Mail = currentUser.Email,
                Phone = currentUser.PhoneNumber,
                Birthdate = currentUser.Birthdate,
                City = currentUser.City,
                Gender = currentUser.Gender
            };
            return View(userEditViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UserEdit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
            currentUser.UserName = model.UserName;
            currentUser.Email = model.Mail;
            currentUser.PhoneNumber = model.Phone;
            currentUser.City = model.City;
            currentUser.Gender = model.Gender;
            currentUser.Birthdate = model.Birthdate;

            if (model.Photo != null && model.Photo.Length > 0)
            {
                var wwwrootFolder = _fileProvider.GetDirectoryContents("wwwroot");
                var randomPhotoName = $"{Guid.NewGuid().ToString()}{Path.GetExtension(model.Photo.FileName)}";
                var newPhotoPath = Path.Combine(wwwrootFolder.First(x => x.Name == "UserPhoto").PhysicalPath, randomPhotoName);

                using var stream = new FileStream(newPhotoPath, FileMode.Create);
                await model.Photo.CopyToAsync(stream);
                currentUser.Photo = randomPhotoName;
            }

            var updateUser = await _userManager.UpdateAsync(currentUser);
            if (!updateUser.Succeeded)
            {
                ModelState.AddModelErrorList(updateUser.Errors);
                return View();
            }

            await _userManager.UpdateSecurityStampAsync(currentUser);
            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(currentUser, true);

            TempData["SuccessMessage"] = "Üye bilgileri başarıyla değiştirilmiştir.";

            return View(model);
        }

        public IActionResult AccessDenied(string ReturnUrl)
        {
            string message = string.Empty;
            message = "Bu sayfa için yetkiniz yok !";
            @ViewBag.message = message;
            return View();
        }
        public async Task<IActionResult> AracKirala(int id)
        {
            AracDurumu aracDurumu = new AracDurumu
            {
                AracId = id,
                BaslangicTarihi = DateTime.Now,
                BitisTarihi = DateTime.Now.AddDays(1)
            };
            return View(aracDurumu);

        }

        [HttpPost]
        public async Task<IActionResult> AracKirala(AracDurumu model)
        {
            var result = _context.AracDurumu.Add(model);

            var currentUser = await _userManager.FindByNameAsync(User.Identity!.Name!);
            model.KullanciId = currentUser.Id;
            TimeSpan time = model.BitisTarihi.Subtract(model.BaslangicTarihi);
            var arac = await _context.Arac.FirstOrDefaultAsync(a => a.Id == model.AracId);
            arac.KiradaMi = true;
            _context.Update(arac);
            model.ToplamUcret = Convert.ToInt32(time.Days) * Convert.ToInt32(arac.Ucret);
            model.Id = 0;
            _context.AracDurumu.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MemberController.Index));

        }

    }
}
