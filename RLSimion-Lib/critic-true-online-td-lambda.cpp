#include "stdafx.h"
#include "critic.h"
#include "vfa.h"
#include "parameters.h"
#include "parameter.h"
#include "features.h"
#include "etraces.h"
#include "globals.h"
#include "experiment.h"
#include "vfa-critic.h"

CTrueOnlineTDLambdaCritic::CTrueOnlineTDLambdaCritic(CParameters *pParameters)
	: CVFACritic(pParameters)
{
	m_e= new CETraces(pParameters->getChild("E-Traces"));
	m_aux= new CFeatureList();
	m_v_s= 0.0;
}

CTrueOnlineTDLambdaCritic::~CTrueOnlineTDLambdaCritic()
{
	delete m_e;
	delete m_aux;
}

double CTrueOnlineTDLambdaCritic::updateValue(CState *s, CAction *a, CState *s_p, double r,double rho)
{
	double v_s_p;

	if (m_pParameters->getParameter("ALPHA")->getDouble()) return 0.0;
	
	if (g_pExperiment->m_expProgress.isFirstStep())
	{
		m_e->clear();
		//vs= theta^T * phi(s)
		m_pVFA->getFeatures(s,0,m_aux);
		m_v_s= m_pVFA->getValue(m_aux);
	}
		
	//vs_p= theta^T * phi(s_p)
	m_pVFA->getFeatures(s_p,0,m_aux);
	v_s_p= m_pVFA->getValue(m_aux);

	double gamma = m_pParameters->getParameter("GAMMA")->getDouble();
	double alpha = m_pParameters->getParameter("ALPHA")->getDouble();
	//delta= R + gamma* v_s_p - v_s
	double td = r + gamma*v_s_p - m_v_s;

	//e= gamma*lambda*e + alpha*[1-gamma*lambda* e^T*phi(s)]* phi(s)
	m_pVFA->getFeatures(s,0,m_aux);										//m_aux <- phi(s)
	double e_T_phi_s= m_e->innerProduct(m_aux);


	m_e->update(gamma);
	double lambda = m_e->getParameters()->getParameter("LAMBDA")->getDouble();
	m_e->addFeatureList(m_aux,alpha *(1-gamma*lambda*e_T_phi_s));

	//theta= theta + delta*e + alpha[v_s - theta^T*phi(s)]* phi(s)
	m_pVFA->add(m_e,td);
	double theta_T_phi_s= m_pVFA->getValue(m_aux);
	m_pVFA->add(m_aux,alpha *(m_v_s - theta_T_phi_s));
	//v_s= v_s_p
	m_v_s= v_s_p;

	return td;
}

