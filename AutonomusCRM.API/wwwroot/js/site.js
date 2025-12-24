function toggleSidebar(){
  document.body.classList.toggle("sidebar-open");
}

// Auto-hide sidebar on mobile when clicking outside
document.addEventListener('click', function(event) {
  const sidebar = document.querySelector('.sidebar');
  const toggle = document.querySelector('.mobile-toggle');
  
  if (window.innerWidth <= 860) {
    if (!sidebar.contains(event.target) && !toggle.contains(event.target)) {
      document.body.classList.remove('sidebar-open');
    }
  }
});

