const header = document.querySelector(".site-header");

const updateHeader = () => {
  if (!header) {
    return;
  }

  const active = window.scrollY > 24;
  header.style.background = active
    ? "rgba(7, 11, 20, 0.92)"
    : "linear-gradient(180deg, rgba(7, 11, 20, 0.84), rgba(7, 11, 20, 0))";
  header.style.backdropFilter = active ? "blur(14px)" : "none";
};

updateHeader();
window.addEventListener("scroll", updateHeader, { passive: true });
