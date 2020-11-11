/*
 * Digital Future Systems LLC, Russia, Perm
 * 
 * This file is part of dfs.reporting.olap.
 * 
 * dfs.reporting.olap is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * dfs.reporting.olap is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with dfs.reporting.olap.  If not, see <https://www.gnu.org/licenses/>.
 */

using Dfs.Reporting.Olap.Models;
using Microsoft.EntityFrameworkCore;

namespace Dfs.Reporting.Olap.Services
{
    public class MetadataDatabaseContext : DbContext
    {
        public MetadataDatabaseContext(DbContextOptions<MetadataDatabaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<NavObject> NavObjects { get; set; }
        public virtual DbSet<VDistrictParent> DistrictParents { get; set; }
        public virtual DbSet<VTeacherClass> TeacherClasses { get; set; }
        public virtual DbSet<VTeacherRsaa> TeacherRsaa { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NavObject>(entity =>
            {
                entity.HasKey(e => e.Code)
                    .HasName("pk_nav_object");

                entity.ToTable("nav_object", "report");

                entity.Property(e => e.Code)
                    .HasColumnName("code")
                    .HasMaxLength(50);

                entity.Property(e => e.ObjectMeta).HasColumnName("object_meta");
            });

            modelBuilder.Entity<VDistrictParent>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("v_district_parent", "report");

                entity.Property(e => e.DistrictParentId).HasColumnName("district_parent_id");

                entity.Property(e => e.Id).HasColumnName("id");
            });

            modelBuilder.Entity<VTeacherClass>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("group_teacher_assignments");

                entity.Property(e => e.ClassUnitId).HasColumnName("class_unit_id");
            });

            modelBuilder.Entity<VTeacherRsaa>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mv_teacher_rsaa");

                entity.Property(e => e.Profiles).HasColumnName("profiles");
            });
        }
    }
}
