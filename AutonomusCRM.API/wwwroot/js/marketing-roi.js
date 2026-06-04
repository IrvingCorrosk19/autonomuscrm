(function () {
  function fmt(n) {
    if (n >= 1e6) return '$' + (n / 1e6).toFixed(1) + 'M';
    if (n >= 1e3) return '$' + Math.round(n / 1e3) + 'k';
    return '$' + Math.round(n).toLocaleString('es');
  }

  function calc() {
    var arr = parseFloat(document.getElementById('roi-arr').value) || 0;
    var churnPct = parseFloat(document.getElementById('roi-churn').value) || 0;
    var atRiskPct = parseFloat(document.getElementById('roi-atrisk').value) || 0;
    var renewalPct = parseFloat(document.getElementById('roi-renewal').value) || 0;
    var improvementPct = parseFloat(document.getElementById('roi-improvement').value) || 0;

    var churnRevenue = arr * (churnPct / 100);
    var atRiskRevenue = arr * (atRiskPct / 100);
    var renewalWindow = arr * (renewalPct / 100);

    var churnSaved = churnRevenue * 0.35 * (improvementPct / 100);
    var renewalsRecovered = renewalWindow * 0.12 * (improvementPct / 100);
    var protectedRev = atRiskRevenue * 0.22 * (improvementPct / 100);
    var total = churnSaved + renewalsRecovered + protectedRev;

    document.getElementById('roi-protected').textContent = fmt(protectedRev);
    document.getElementById('roi-churn-saved').textContent = fmt(churnSaved);
    document.getElementById('roi-renewals').textContent = fmt(renewalsRecovered);
    document.getElementById('roi-total').textContent = fmt(total);
  }

  ['roi-arr', 'roi-churn', 'roi-atrisk', 'roi-renewal', 'roi-improvement'].forEach(function (id) {
    var el = document.getElementById(id);
    if (el) el.addEventListener('input', calc);
  });
  calc();
})();
