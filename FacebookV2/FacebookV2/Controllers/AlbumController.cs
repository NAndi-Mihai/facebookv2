﻿using FacebookV2.App_Start;
using FacebookV2.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FacebookV2.Controllers
{
    [Authorize]
    public class AlbumController : Controller
    {
        private ApplicationDbContext db = ApplicationDbContext.Create();

        public AlbumController()
        {
        }

        [HttpGet]
        public ActionResult Show()
        {
            var currentUserId = User.Identity.GetUserId();
            var userAlbums = GetUserAlbums(currentUserId);

            var albumsToShow = new List<ShowAlbumViewModel>();
            albumsToShow = userAlbums.Select(album => new ShowAlbumViewModel
            {
                Id = album.Id,
                Name = album.Name,
                NumberOfPhotos = album.Photos.Count,
                FirstPhotoId = album.Photos.FirstOrDefault()?.Id,
                IsEditable = true
            })
            .ToList();

            var model = new EditPhotosViewModel()
            {
                Albums = albumsToShow
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateAlbum(AddAlbumViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Show", "Album");
            }

            try
            {
                var currentUserId = User.Identity.GetUserId();
                var newAlbum = new Album()
                {
                    Name = model.Name,
                    ProfileId = currentUserId,
                    Profile = db.Profiles.Where(p => p.Id == currentUserId).FirstOrDefault()
                };


                db.Albums.Add(newAlbum);
                db.SaveChanges();

                return RedirectToAction("Edit", "Album", new { albumId = newAlbum.Id });
            }
            catch (Exception e)
            {
                return View("InternalServerError");
            }
        }

        [HttpGet]
        public ActionResult Edit(long albumId)
        {
            var album = GetAlbumById(albumId);
            if (album == null)
                return View("NotFound");

            var model = new EditAlbumViewModel()
            {
                Id = album.Id,
                Name = album.Name,
                Photos = new List<PhotoViewModel>(album.Photos.Select(u => new PhotoViewModel
                {
                    Id = u.Id,
                    Caption = u.Caption
                }).ToList())
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(EditAlbumViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /*---------Adaugam o poza noua albumului ---------*/
            if (model.NewPhoto != null)
            {
                //AddPhoto(model);
                var currentUserId = User.Identity.GetUserId();

                MemoryStream target = new MemoryStream();
                model.NewPhoto.InputStream.CopyTo(target);
                byte[] data = target.ToArray();

                var photoToAdd = new Photo()
                {
                    Content = data,
                    ProfileId = currentUserId,
                    Profile = db.Profiles.Where(p => p.Id == currentUserId).FirstOrDefault(),
                    AlbumId = model.Id,
                    Album = db.Albums.Where(a => a.Id == model.Id).FirstOrDefault()
                };

                db.Photos.Add(photoToAdd);
                db.SaveChanges();

                model.Photos.Add(new PhotoViewModel
                {
                    Id = GetPictureById(photoToAdd.Id).Id
                });
            }


            /* --------- Updatam numele albumului ---------*/
            var currentAlbum = db.Albums.Where(u => u.Id == model.Id)
                                        .Include(p => p.Photos)
                                        .FirstOrDefault();
            if (currentAlbum != null)
                currentAlbum.Name = model.Name;

            db.SaveChanges();


            /* --------- actualizam descrierile pozelor deja existente ---------*/
            foreach (var photo in model.Photos)
            {
                var existingPhoto = GetPictureById(photo.Id);
                if (existingPhoto.Caption != photo.Caption)
                {
                    existingPhoto.Caption = photo.Caption;
                    db.SaveChanges();
                }
            }


            /* --------- Stergem pozele care au fost selectate in acest sens ---------*/
            var picturesToDelete = model.Photos.Where(p => p.IsDeleted)
                                                .Select(p => p.Id)
                                                .ToList();
            foreach (var pictureId in picturesToDelete)
            {
                var photoEntity = GetPictureById(pictureId);

                db.Photos.Remove(photoEntity);
                db.SaveChanges();
            }

            return RedirectToAction("Edit", "Album", new { albumId = model.Id });
        }

        [HttpPost]
        public ActionResult Delete(long albumId)
        {
            try
            {
                var albumToDelete = GetAlbumById(albumId);
                if (albumToDelete == null)
                    return View("NotFound");

                db.Albums.Remove(albumToDelete);

                db.SaveChanges();

                return new EmptyResult();
            }
            catch
            {
                return View("InternalServerError");
            }
        }

        [NonAction]
        public List<Album> GetUserAlbums(string userId)
        {
            return db.Albums.Where(u => u.ProfileId == userId)
                            .Include(u => u.Photos)
                            .ToList();
        }

        [NonAction]
        public Album GetAlbumById(long albumId)
        {
            return db.Albums.Where(u => u.Id == albumId)
                             .Include(p => p.Photos)
                             .FirstOrDefault();
        }

        [NonAction]
        public Photo GetPictureById(long? pictureId)
        {
            return db.Photos.Where(u => u.Id == pictureId)
                            .FirstOrDefault();
        }

        [NonAction]
        public void AddPhoto(EditAlbumViewModel model)
        {
            var currentUserId = User.Identity.GetUserId();

            MemoryStream target = new MemoryStream();
            model.NewPhoto.InputStream.CopyTo(target);
            byte[] data = target.ToArray();

            var photoToAdd = new Photo()
            {
                Content = data,
                ProfileId = currentUserId,
                Profile = db.Profiles.Where(p => p.Id == currentUserId).FirstOrDefault(),
                AlbumId = model.Id,
                Album = db.Albums.Where(a => a.Id == model.Id).FirstOrDefault()
            };

            db.Photos.Add(photoToAdd);
            //db.SaveChanges();

            model.Photos.Add(new PhotoViewModel
            {
                Id = GetPictureById(photoToAdd.Id).Id
            });
        }
    }
}